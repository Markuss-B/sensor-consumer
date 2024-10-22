namespace MqqtConsumer.Models;

public class SensorMeasurements
{
    public int Id { get; set; } // Unique identifier for each measurement record
    public DateTime Timestamp { get; set; } // Time the measurement was recorded
    public string SensorId { get; set; } // Reference to the sensor metadata
    public Sensor Sensor { get; set; } // Navigation property to the sensor metadata
    public float? CO2 { get; set; } // CO2 level
    public float? Temperature { get; set; } // Temperature in Celsius
    public float? Battery { get; set; } // Battery level as a percentage or voltage
    public int? AtmosphericPressure { get; set; } // Atmospheric pressure in Pascals
    public float? Rssi { get; set; } // Signal strength in dBm
}

