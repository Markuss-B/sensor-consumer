using Microsoft.Extensions.Options;
using SensorMonitoring.Consumer.Models;
using Newtonsoft.Json.Linq;
using SensorMonitoring.Consumer.Configuration;

namespace SensorMonitoring.Consumer.Services;

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
        string[] topicParts = topic.Split('/');
        string sensorId = topicParts[_sensorIdPosition];

        if (_sensorService.IsSensorInactive(sensorId))
        {
            _logger.LogInformation("Sensor with ID '{SensorId}' is inactive. Skipping message processing.", sensorId);
            return;
        }

        string payloadType = topicParts.Last();

        List<Task> tasks = [];

        tasks.Add(HandleTopic(sensorId, topic));

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
            _logger.LogInformation("Not processing unknown topic: {topic}", topic);
        else
            tasks.Add(handlerTask);

        await Task.WhenAll(tasks);
    }

    private async Task HandleTopic(string sensorId, string topic)
    {
        var tasks = new List<Task>();

        tasks.Add(_sensorService.UpdateSensorTopicsAsync(sensorId, topic));

        string[] topicParts = topic.Split('/');

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
        var tasks = new List<Task>();

        JObject jsonDocument = JObject.Parse(payload);

        //tasks.Add(_sensorService.SaveMeasurementsRawAsync(sensorId, jsonDocument.ToString()));

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

        tasks.Add(_sensorService.SaveMeasurementsAsync(sensorId, timestamp, jsonString));

        await Task.WhenAll(tasks);
    }

    // Generic handler for sensor metadata
    private async Task HandleSensorMetadataAsync(string sensorId, string payload, string metadataType)
    {
        await _sensorService.UpdateSensorMetadataAsync(sensorId, metadataType, payload);
    }
}
