using MongoDB.Bson;
using MongoDB.Driver;
using SensorMonitoring.Consumer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitoring.Consumer.Data;

public class InactiveSensorCache
{
    private readonly ILogger<InactiveSensorCache> _logger;
    private ConcurrentDictionary<string, byte> _inactiveSensors;

    public InactiveSensorCache(ILogger<InactiveSensorCache> logger)
    {
        _logger = logger;
    }

    public bool IsSensorInactive(string sensorId)
    {
        return _inactiveSensors.ContainsKey(sensorId);
    }

    public void LoadInactiveSensors(IEnumerable<string> sensors)
    {
        _inactiveSensors = new ConcurrentDictionary<string, byte>(sensors.Select(s => new KeyValuePair<string, byte>(s, 0)));
        _logger.LogInformation("Loaded inactive sensors: {InactiveSensors}.", string.Join(", ", _inactiveSensors.Keys));
    }

    public void AddInactiveSensor(string sensorId)
    {
        _inactiveSensors.TryAdd(sensorId, 0);
        _logger.LogInformation("Sensor {SensorId} is now inactive.", sensorId);
    }

    public void RemoveInactiveSensor(string sensorId)
    {
        _inactiveSensors.TryRemove(sensorId, out _);
        _logger.LogInformation("Sensor {SensorId} is now active.", sensorId);
    }

    public void Clear()
    {
        _inactiveSensors.Clear();
        _logger.LogInformation("Cleared inactive sensors.");
    }
}
