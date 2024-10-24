namespace MqttConsumer.Configuration;
public class MqttSettings
{
    public string Broker { get; set; }
    public string ProtocolVersion { get; set; }
    public string TopicFilter { get; set; }
    public string TopicSchema { get; set; }
    public string[] SplitTopicSchema { get; set; }  // Store the split schema
    public int SensorIdPosition { get; set; }  // Store the index of 'sensorId'
}
