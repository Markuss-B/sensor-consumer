using System.Collections.Concurrent;

namespace SensorConsumer.Data;

public class InactiveSensorCache
{
    private readonly ILogger<InactiveSensorCache> _logger;
    private ConcurrentDictionary<string, byte> _inactiveSensors;

    public InactiveSensorCache(ILogger<InactiveSensorCache> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if the sensor is set inactive by checking the inactive sensor cache.
    /// </summary>
    public bool IsSensorInactive(string sensorId)
    {
        return _inactiveSensors.ContainsKey(sensorId);
    }

    /// <summary>
    /// Load the sensors set as inactive in to cache.
    /// </summary>
    public void LoadInactiveSensors(IEnumerable<string> sensors)
    {
        _inactiveSensors = new ConcurrentDictionary<string, byte>(sensors.Select(s => new KeyValuePair<string, byte>(s, 0)));
        _logger.LogInformation("Loaded inactive sensors: {InactiveSensors}.", string.Join(", ", _inactiveSensors.Keys));
    }

    /// <summary>
    /// Add a sensor to the inactive sensor cache.
    /// </summary>
    public void AddInactiveSensor(string sensorId)
    {
        _inactiveSensors.TryAdd(sensorId, 0);
        _logger.LogInformation("Sensor {SensorId} is now inactive.", sensorId);
    }

    /// <summary>
    /// Remove a sensor from the inactive sensor cache.
    /// </summary>
    public void RemoveInactiveSensor(string sensorId)
    {
        _inactiveSensors.TryRemove(sensorId, out _);
        _logger.LogInformation("Sensor {SensorId} is now active.", sensorId);
    }

    /// <summary>
    /// Clear the inactive sensor cache.
    /// </summary>
    public void Clear()
    {
        _inactiveSensors.Clear();
        _logger.LogInformation("Cleared inactive sensors.");
    }
}
