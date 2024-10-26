﻿using MongoDB.Bson;
using MongoDB.Driver;
using MqttConsumer.Data;
using MqttConsumer.Models;

namespace MqttConsumer.Services;

public class SensorService
{
    private readonly ILogger<SensorService> _logger;
    private readonly MongoDbContext _context;

    public SensorService(ILogger<SensorService> logger, MongoDbContext context)
    {
        _logger = logger;
        _context = context;
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

    public async Task UpdateSensorMetadataAsync(string sensorId, string fieldName, string newValue)
    {
        var filter = Builders<Sensor>.Filter.Eq(s => s.Id, sensorId);
        var update = Builders<Sensor>.Update.Set(fieldName, newValue);

        // Set the upsert option to true
        var updateOptions = new UpdateOptions { IsUpsert = true };

        var result = await _context.sensors.UpdateOneAsync(filter, update, updateOptions);

        if (result.UpsertedId != null)
        {
            _logger.LogInformation("A new document was inserted with ID '{DocumentId}'.", result.UpsertedId);
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


}
