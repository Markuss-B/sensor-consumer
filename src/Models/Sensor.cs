using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorConsumer.Models;

[BsonIgnoreExtraElements]
public class Sensor
{
    [BsonId]
    public string Id { get; set; } // Unique ID for the sensor
    [BsonElement("topics")]
    public HashSet<string> Topics { get; set; } // List of topics for the sensor
    [BsonElement("isActive")]
    public bool IsActive { get; set; } // Flag indicating if sensor messages should be processed
    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } // Metadata for the sensor
    [BsonElement("latestMeasurementTimestamp")]
    public DateTime LatestMeasurementTimestamp { get; set; } // Timestamp of the last measurement
    [BsonElement("latestMeasurements")]
    public BsonDocument LatestMeasurements { get; set; } // Timestamp of the last measurement
}

