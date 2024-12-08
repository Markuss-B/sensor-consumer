using MongoDB.Bson.Serialization.Attributes;

namespace MqttConsumer.Models;

public class Sensor
{
    [BsonId]
    public string Id { get; set; } // Unique ID for the sensor
    [BsonElement("topics")]
    public HashSet<string> Topics { get; set; } // List of topics for the sensor
    public string? Name { get; set; } // Sensor name
    [BsonElement("productNumber")]
    public string? ProductNumber { get; set; } // Product number of the sensor
    [BsonElement("group")]
    public string? Group { get; set; } // Group identifier for the sensor
    [BsonElement("groupId")]
    public string? GroupId { get; set; } // Group ID of the sensor
}

