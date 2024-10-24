using Microsoft.Extensions.Options;
using MqttConsumer.Configuration;
using Newtonsoft.Json.Linq;

namespace MqttConsumer.Services;

public class MqttMessageProcessingService
{
    private readonly ILogger<MqttMessageProcessingService> _logger;
    private readonly SensorService _sensorService;
    private readonly string[] _topicSchema;
    private readonly int _sensorIdPosition;

    public MqttMessageProcessingService(ILogger<MqttMessageProcessingService> logger, SensorService sensorService, IOptions<MqttSettings> options)
    {
        _logger = logger;
        _sensorService = sensorService;
        _topicSchema = options.Value.SplitTopicSchema;
        _sensorIdPosition = options.Value.SensorIdPosition;
    }

    // Main method that receives the message and delegates to the appropriate handler
    public async Task ProcessMessageAsync(string topic, string payload)
    {
        // Extract the last part of the topic to identify the payload type
        string[] topicParts = topic.Split('/');
        string sensorId = topicParts[_sensorIdPosition];
        string payloadType = topicParts.Last();

        List<Task> tasks = [];

        tasks.Add(HandleTopic(sensorId, topicParts));

        // Use a switch expression to route to the appropriate handler
        var handlerTask = payloadType switch
        {
            "measurements" => HandleMeasurementsAsync(sensorId, payload),
            "name" => HandleSensorMetadataAsync(sensorId, payload, "name"),
            "productNumber" => HandleSensorMetadataAsync(sensorId, payload, "productNumber"),
            "group" => HandleSensorMetadataAsync(sensorId, payload, "group"),
            "groupId" => HandleSensorMetadataAsync(sensorId, payload, "groupId"),
            _ => null
        };

        if (handlerTask == null)
            _logger.LogWarning("Not processing unknown topic: {topic}", topic);
        else
            tasks.Add(handlerTask);

        await Task.WhenAll(tasks);
    }

    private async Task HandleTopic(string sensorId, string[] topicParts)
    {
        var tasks = new List<Task>();

        for (int i = 0; i < _topicSchema.Length; i++)
        {
            string topicPart = topicParts[i];
            string schemaPart = _topicSchema[i];

            // We have reached the sensorId part of the topic we don't care about the rest
            if (schemaPart == "sensorId")
                break;

            // If the schema part is empty we don't care about it
            if (string.IsNullOrEmpty(schemaPart))
                continue;

            // named parts in schema get saved as metadata
            // update sensor where field is schemaPart and value is topicPart
            tasks.Add(_sensorService.UpdateSensorMetadataAsync(sensorId, schemaPart, topicPart));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Handles measurments. Topics like: Aranetest/394260700033/sensors/3002FA/json/measurements
    /// </summary>
    /// <param name="topic">Measurment topic</param>
    /// <param name="payload">Measurment payload - json string containing measurments like co2, temperature, battery.</param>
    /// <returns></returns>
    private async Task HandleMeasurementsAsync(string sensorId, string payload)
    {
        JObject jsonDocument = JObject.Parse(payload);

        long unixTime;
        DateTime timestamp;

        if (jsonDocument.TryGetValue("time", out JToken timeToken) && long.TryParse(timeToken.ToString(), out unixTime))
        {
            int unixTimeLength = unixTime.ToString().Length;
            if (unixTimeLength == 10)
            {
                timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            }
            else if (unixTimeLength == 13)
            {
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).UtcDateTime;
            }
            else
            {
                _logger.LogError("Invalid time format in payload data.");
                return;
            }

            jsonDocument.Remove("time");
        }
        else
        {
            _logger.LogError("Error parsing time from payload data.");
            return;
        }

        string jsonString = jsonDocument.ToString();

        await _sensorService.SaveMeasurementsAsync(sensorId, timestamp, jsonString);
    }

    // Generic handler for sensor metadata
    private async Task HandleSensorMetadataAsync(string sensorId, string payload, string metadataType)
    {
        await _sensorService.UpdateSensorMetadataAsync(sensorId, metadataType, payload);
    }
}
