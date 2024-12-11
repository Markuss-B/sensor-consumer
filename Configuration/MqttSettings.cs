using MQTTnet.Formatter;
using System.Security.Authentication;

namespace SensorMonitoring.Consumer.Configuration;
public class MqttSettings
{
    public string Broker { get; set; }
    public MqttProtocolVersion ProtocolVersion { get; set; }
    public string TopicFilter { get; set; }
    public bool UseTls { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string PathToPem { get; set; }
    public SslProtocols SslProtocol { get; set; }
    public string TopicSchema { get; set; }
    public string[] SplitTopicSchema { get; set; }  // Store the split schema
    public int SensorIdPosition { get; set; }  // Store the index of 'sensorId'
}
