using MqttConsumer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MqttConsumer.Models;

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public class SensorMeasurementsPayload
{
    public int CO2 { get; set; }
    public float Rssi { get; set; }
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime Time { get; set; }
    public float Battery { get; set; }
    public float Temperature { get; set; }
    public int AtmosphericPressure { get; set; }
}
