using MQTTnet.Formatter;
using SensorConsumer.Models;
using System.Security.Authentication;

namespace SensorConsumer.Configuration;
public class MqttSettings
{
    public string Broker { get; set; }
    public MqttProtocolVersion ProtocolVersion { get; set; }
    public bool UseTls { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string PathToPem { get; set; }
    public SslProtocols SslProtocol { get; set; }
    public string Topics { get; set; } // "Aranetest/+/sensors/<sensorId>/json/measurements, Aranetest/+/sensors/<sensorId>/<metadataName>"
    public List<TopicSchema> TopicSchemas { get; set; }
}

public class TopicSchema
{
    public string TopicFilter { get; set; } // "Aranetest/+/sensors/+/json/measurements"
    public TopicType TopicType { get; set; }
    public int SensorIdPosition { get; set; } // 3
    public int MetadataNamePosition { get; set; } // 
}

public enum TopicType
{
    Measurements,
    Metadata
}

/** Examples of TopicSchemas:
 * **
 * Input topic: "Aranetest/+/sensors/<sensorId>/json/measurements"
 * Topic: "Aranetest/+/sensors/+/json/measurements"
 * TopicType: Measurements
 * SensorIdPosition: 3
 * MetadataNamePosition: null
 * 
 * **
 * Input topic: "Aranetest/+/sensors/<sensorId>/<metadataName>"
 * Topic: "Aranet/+/sensors/+/+"
 * TopicType: Metadata
 * SensorIdPosition: 3
 * MetadataNamePosition: 4
 * 
 * **
 * Input topic: "test/<sensorId>/measurements"
 * Topic: "test/+/measurements"
 * TopicType: Measurements
 * SensorIdPosition: 1
 * 
 * **
 * Input topic: "test/<sensorId>/<metadataName>"
 * Topic: "test/+/+"
 * TopicType: Metadata
 * SensorIdPosition: 1
 * MetadataNamePosition: 2
 */