using MongoDB.Bson;
using MongoDB.Driver;
using MqttConsumer.Data;
using MqttConsumer.Models;
using MQTTnet.Internal;

namespace MqttConsumer.Services;

public class SensorService
{
    private readonly ILogger<SensorService> _logger;
    private readonly MongoDbContext _context;
    private readonly InactiveSensorCache _inactiveSensorCache;

    public SensorService(ILogger<SensorService> logger, MongoDbContext context, InactiveSensorCache inactiveSensorCache)
    {
        _logger = logger;
        _context = context;
        _inactiveSensorCache = inactiveSensorCache;
    }

    public bool IsSensorInactive(string sensorId)
    {
        return _inactiveSensorCache.IsSensorInactive(sensorId);
    }

    public async Task SaveMeasurementsAsync(string sensorId, DateTime timestamp, string jsonString)
    {
        // Convert the JSON data into a BsonDocument
        var measurements = BsonDocument.Parse(jsonString);

        // Create a SensorMeasurement object
        var sensorMeasurement = new SensorMeasurements
        {
            SensorId = sensorId,
            Timestamp = timestamp,
            Measurements = measurements
        };

        // Insert the document into MongoDB
        await _context.sensorMeasurements.InsertOneAsync(sensorMeasurement);

        _logger.LogInformation("Saved measurements for sensor with ID '{SensorId}' with timestamp '{Timestamp}'.", sensorId, timestamp);
    }

    //public async Task SaveMeasurementsRawAsync(string sensorId, string jsonString)
    //{ 
    //    var measurements = BsonDocument.Parse(jsonString);

    //    // Create a SensorMeasurement object
    //    var sensorMeasurementRaw = new SensorMeasurementsRaw
    //    {
    //        SensorId = sensorId,
    //        Measurements = measurements
    //    };

    //    // Insert the document into MongoDB
    //    await _context.sensorMeasurementsRaw.InsertOneAsync(sensorMeasurementRaw);

    //    _logger.LogInformation("Saved raw measurements for sensor with ID '{SensorId}'.", sensorId);
    //}

    public async Task UpdateSensorMetadataAsync(string sensorId, string fieldName, string newValue)
    {
        var filter = Builders<Sensor>.Filter.Eq(s => s.Id, sensorId);
        var update = Builders<Sensor>.Update
            .Set(fieldName, newValue)
            .CurrentDate(s => s.LastUpdated);

        // Set the upsert option to true
        var updateOptions = new UpdateOptions { IsUpsert = true };

        var result = await _context.sensors.UpdateOneAsync(filter, update, updateOptions);

        if (result.UpsertedId != null)
        {
            _logger.LogInformation("A new document was inserted with ID '{DocumentId}' and field '{FieldName}' with value '{NewValue}'.", result.UpsertedId, fieldName, newValue);
        }
        else if (result.ModifiedCount == 0)
        {
            _logger.LogInformation("Sensor with ID '{SensorId}' already had the same value for the field '{FieldName}'. No changes were made.", sensorId, fieldName);
        }
        else
        {
            _logger.LogInformation("Successfully updated sensor with ID '{SensorId}' and field '{FieldName}' to '{NewValue}'.", sensorId, fieldName, newValue);
        }
    }

    public async Task UpdateSensorTopicsAsync(string sensorId, string topic)
    {
        var filter = Builders<Sensor>.Filter.Eq(s => s.Id, sensorId);
        var update = Builders<Sensor>.Update
            .AddToSet(s => s.Topics, topic)
            .CurrentDate(s => s.LastUpdated);

        var updateOptions = new UpdateOptions { IsUpsert = true };

        var result = await _context.sensors.UpdateOneAsync(filter, update, updateOptions);

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
}
