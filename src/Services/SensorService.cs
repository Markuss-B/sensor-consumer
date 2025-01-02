using MongoDB.Bson;
using MongoDB.Driver;
using SensorConsumer.Data;
using SensorConsumer.Models;

namespace SensorConsumer.Services;

/// <summary>
/// Service to handle sensor data. Saves sensor measurements and metadata to the MongoDB database.
/// </summary>
public class SensorService
{
    private readonly ILogger<SensorService> _logger;
    private readonly MongoDb _db;
    private readonly InactiveSensorCache _inactiveSensorCache;

    public SensorService(ILogger<SensorService> logger, MongoDb db, InactiveSensorCache inactiveSensorCache)
    {
        _logger = logger;
        _db = db;
        _inactiveSensorCache = inactiveSensorCache;
    }

    /// <summary>
    /// Check if the inactive sensor cache contains the sensor.
    /// </summary>
    public bool IsSensorInactive(string sensorId)
    {
        return _inactiveSensorCache.IsSensorInactive(sensorId);
    }

    /// <summary>
    /// Save the measurements to the database:
    /// Saves the measurements to the <see cref="MongoDb.sensorMeasurements"/> collection and 
    /// the sensor document in <see cref="MongoDb.sensors"/> collection with the latest measurements.
    /// </summary>
    public async Task SaveMeasurementsAsync(string sensorId, DateTime timestamp, BsonDocument measurements)
    {
        // Create a SensorMeasurement object
        var sensorMeasurement = new SensorMeasurements
        {
            SensorId = sensorId,
            Timestamp = timestamp,
            Measurements = measurements
        };

        List<Task> tasks = new();
        // Insert the measurements into the sensorMeasurements collection
        tasks.Add(_db.sensorMeasurements.InsertOneAsync(sensorMeasurement));

        // Update the sensor document with the latest measurements
        var filter = Builders<Sensor>.Filter.Eq(s => s.Id, sensorId);
        var update = Builders<Sensor>.Update
            .Set(s => s.LatestMeasurements, measurements)
            .Set(s => s.LatestMeasurementTimestamp, timestamp);

        tasks.Add(_db.sensors.UpdateOneAsync(filter, update));

        await Task.WhenAll(tasks);

        _logger.LogInformation("Saved measurements for sensor with ID '{SensorId}' with timestamp '{Timestamp}'.", sensorId, timestamp);
    }

    /// <summary>
    /// Update the sensor topics of the sensor.
    /// </summary>
    public async Task UpdateSensorTopicsAsync(string sensorId, string topic)
    {
        var filter = Builders<Sensor>.Filter.Eq(s => s.Id, sensorId);
        var update = Builders<Sensor>.Update
            .AddToSet(s => s.Topics, topic);

        var updateOptions = new UpdateOptions { IsUpsert = true };

        var result = await _db.sensors.UpdateOneAsync(filter, update, updateOptions);

        if (result.UpsertedId != null)
        {
            _logger.LogInformation("A new document was inserted with ID '{DocumentId}' and topic '{Topic}'.", result.UpsertedId, topic);
        }
        else if (result.ModifiedCount == 0)
        {
            _logger.LogInformation("Sensor with ID '{SensorId}' already had the topic '{Topic}'. No changes were made.", sensorId, topic);
        }
        else
        {
            _logger.LogInformation("Successfully added topic '{Topic}' to sensor with ID '{SensorId}'.", topic, sensorId);
        }
    }

    /// <summary>
    /// Update the sensor metadata:
    /// Saves the metadata to the the sensor document in <see cref="MongoDb.sensors"/> collection
    /// and if the metadata is new, adds it to the <see cref="MongoDb.sensorMetadatas"/> collection to keep a history of the metadata changes.
    /// </summary>
    public async Task UpdateSensorMetadataAsync(string sensorId, string fieldName, string newValue)
    {
        var filter = Builders<Sensor>.Filter.Eq(s => s.Id, sensorId);
        var update = Builders<Sensor>.Update
            .Set(s => s.Metadata[fieldName], newValue);

        // Upsert so that a new document is created if the sensor does not exist
        var updateOptions = new UpdateOptions { IsUpsert = true };

        var result = await _db.sensors.UpdateOneAsync(filter, update, updateOptions);

        if (result.UpsertedId != null)
        {
            // A new document was created
            await SaveMetadataHistory(sensorId, fieldName, newValue);
            _logger.LogInformation("A new document was inserted with ID '{DocumentId}' and field '{FieldName}' with value '{NewValue}'.", result.UpsertedId, fieldName, newValue);
        }
        else if (result.ModifiedCount > 0)
        {
            // An existing sensor was updated
            await SaveMetadataHistory(sensorId, fieldName, newValue);
            _logger.LogInformation("Successfully updated sensor with ID '{SensorId}' and field '{FieldName}' to '{NewValue}'.", sensorId, fieldName, newValue);
        }
        else
        {
            _logger.LogInformation("Sensor with ID '{SensorId}' already had the same value for the field '{FieldName}'. No changes were made.", sensorId, fieldName);
        }
    }

    private async Task SaveMetadataHistory(string sensorId, string fieldName, string newValue)
    {
        var series = new SensorMetadatas
        {
            SensorId = sensorId,
            Timestamp = DateTime.UtcNow,
            Metadata = new BsonDocument
            {
                { fieldName, newValue }
            }
        };

        await _db.sensorMetadatas.InsertOneAsync(series);

        _logger.LogInformation("Saved metadata history for sensor with ID '{SensorId}' with field '{FieldName}' and value '{NewValue}'.", sensorId, fieldName, newValue);
    }
}
