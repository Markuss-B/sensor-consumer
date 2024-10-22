namespace MqqtConsumer.Models;

public class Sensor
{
    public string Id { get; set; } // Unique ID for the sensor
    public string? BaseSerialNumber { get; set; } // Serial number of the base unit
    public string? RootTopic { get; set; } // Root topic for the sensor
    public string? Name { get; set; } // Sensor name
    public string? ProductNumber { get; set; } // Product number of the sensor
    public string? Group { get; set; } // Group identifier for the sensor
    public string? GroupId { get; set; } // Group ID of the sensor
    public List<SensorMeasurements>? Measurements { get; set; } // Navigation property to the sensor measurements
}

