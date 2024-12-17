using MongoDB.Bson;
using MongoDB.Driver;
using SensorConsumer.Data;
using SensorConsumer.Models;

namespace SensorConsumer.Workers;

/// <summary>
/// Worker to watch for changes in the sensors collection and update the inactive sensor cache.
/// </summary>
public class InactiveSensorCacheWorker : BackgroundService
{
    private readonly ILogger<InactiveSensorCacheWorker> _logger;
    private MongoDb _db;
    private InactiveSensorCache _cache;

    public InactiveSensorCacheWorker(ILogger<InactiveSensorCacheWorker> logger, MongoDb db, InactiveSensorCache inactiveSensorCache)
    {
        _logger = logger;
        _db = db;
        _cache = inactiveSensorCache;
    }

    /// <summary>
    /// Load all inactive sensors from the database and start the worker.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inactive sensor cache starting.");

        // load all inactive sensors from the database
        var filter = Builders<Sensor>.Filter.Eq(s => s.IsActive, false);
        var sensors = _db.sensors.Find(filter).Project(s => s.Id).ToList();

        _cache.LoadInactiveSensors(sensors);

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// The worker watches for changes in the sensors collection and updates the inactive sensor cache.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Inactive sensor cache executing.");

        var col = _db.sensors;
        // Only watch for updates where the isActive field is updated
        var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
        var pipeline = new BsonDocument[]
        {
                new BsonDocument("$match", new BsonDocument("updateDescription.updatedFields.isActive", new BsonDocument("$exists", true)))
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Watching for changes in the sensors collection.");

                // Watch for changes in the sensors collection
                using var cursor = await col.WatchAsync<ChangeStreamDocument<Sensor>>(pipeline, options, cancellationToken);

                // For every change detected, update the cache
                await cursor.ForEachAsync(change =>
                {
                    _logger.LogInformation("Change detected: {Change}", change);
                    if (change.OperationType == ChangeStreamOperationType.Update)
                    {
                        var sensorId = change.FullDocument.Id;
                        var isActive = change.FullDocument.IsActive;

                        if (isActive)
                        {
                            _cache.RemoveInactiveSensor(sensorId);
                        }
                        else
                        {
                            _cache.AddInactiveSensor(sensorId);
                        }
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Inactive sensor cache stopping.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error watching for changes in the sensors collection.");
                await Task.Delay(5000, cancellationToken);
            }
        }

        _logger.LogInformation("Inactive sensor cache execution stopped.");
    }

    /// <summary>
    /// Clear the cache when the worker is stopped.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _cache.Clear();
        await base.StopAsync(cancellationToken);
    }
}
