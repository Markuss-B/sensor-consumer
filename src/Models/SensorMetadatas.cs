﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorConsumer.Models;

public class SensorMetadatas
{
    [BsonId]
    public ObjectId Id { get; set; } // Unique identifier for each measurement record
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } // Time the measurement was recorded
    [BsonElement("sensorId")]
    public string SensorId { get; set; } // Sensor Id given by the sensor
    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } // Measurements from the sensor
}
