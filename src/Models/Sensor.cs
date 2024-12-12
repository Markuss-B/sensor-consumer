using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SensorConsumer.Models;

public class Sensor
{
    [BsonId]
    public string Id { get; set; } // Unique ID for the sensor
    [BsonElement("topics")]
    public HashSet<string> Topics { get; set; } // List of topics for the sensor
    [BsonElement("name")]
    public string? Name { get; set; } // Sensor name
    [BsonElement("productNumber")]
    public string? ProductNumber { get; set; } // Product number of the sensor
    [BsonElement("group")]
    public string? Group { get; set; } // Group identifier for the sensor
    [BsonElement("groupId")]
    public string? GroupId { get; set; } // Group ID of the sensor
    [BsonElement("isActive")]
    public bool IsActive { get; set; } // Flag indicating if sensor messages should be processed
    [BsonExtraElements]
    public BsonDocument? ExtraElements { get; set; } // Additional fields that are not mapped to properties
}

