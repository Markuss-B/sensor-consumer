using Microsoft.Extensions.Options;
using MongoDB.Bson;
using SensorConsumer.Configuration;

namespace SensorConsumer.Services;

/// <summary>
/// Service to process incoming MQTT messages.
/// </summary>
public class ProcessingService
{
    private readonly ILogger<ProcessingService> _logger;
    private readonly SensorService _sensorService;

    private readonly List<TopicSchema> _topicSchemas;

    public ProcessingService(ILogger<ProcessingService> logger, SensorService sensorService, IOptions<MqttSettings> options)
    {
        _logger = logger;
        _sensorService = sensorService;
        _topicSchemas = options.Value.TopicSchemas;
    }

    // Main method that receives the message and delegates to the appropriate handler
    public async Task ProcessMessageAsync(string topic, string payload)
    {
        // Find a TopicSchema that matches the topic
        TopicSchema? schema = MatchTopicSchema(topic);
        if (schema == null)
        {
            _logger.LogInformation("No matching schema found for topic: {topic}", topic);
            return;
        }

        // Split the topic into parts to extract the sensorId
        string[] topicParts = topic.Split('/');
        string sensorId = topicParts[schema.SensorIdPosition];

        if (_sensorService.IsSensorInactive(sensorId))
        {
            _logger.LogInformation("Sensor with ID '{SensorId}' is inactive. Skipping message processing.", sensorId);
            return;
        }

        List<Task> tasks = [];

        tasks.Add(HandleTopic(sensorId, topic));

        var handlerTask = schema.TopicType switch
        {
            TopicType.Measurements => HandleMeasurementsAsync(sensorId, payload),
            TopicType.Metadata => HandleSensorMetadataAsync(sensorId, payload, topicParts[schema.MetadataNamePosition]),
            _ => null
        };

        if (handlerTask == null)
            _logger.LogInformation("Not processing unknown topic: {topic}", topic);
        else
            tasks.Add(handlerTask);

        // The handler methods process and save the data to the database
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Handles sensor topics.
    /// </summary>
    private async Task HandleTopic(string sensorId, string topic)
    {
        await _sensorService.UpdateSensorTopicsAsync(sensorId, topic);
    }

    /// <summary>
    /// Handles sensor metadata. Topics like: Aranetest/394260700033/sensors/3002FA/name
    /// </summary>
    private async Task HandleSensorMetadataAsync(string sensorId, string payload, string metadataType)
    {
        await _sensorService.UpdateSensorMetadataAsync(sensorId, metadataType, payload);
    }

    /// <summary>
    /// Handles measurments. Topics like: Aranetest/394260700033/sensors/3002FA/json/measurements
    /// </summary>
    /// <param name="topic">Measurment topic</param>
    /// <param name="payload">Measurment payload - json string containing measurments like { "time": 1630512000, "temperature": 23.5, "time": "1630512000", }</param>
    /// <returns></returns>
    private async Task HandleMeasurementsAsync(string sensorId, string payload)
    {
        BsonDocument bsonElements = BsonDocument.Parse(payload);

        long unixTime;
        DateTime timestamp;

        if (bsonElements.TryGetValue("time", out BsonValue timeToken) && long.TryParse(timeToken.ToString(), out unixTime))
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

            bsonElements.Remove("time");
        }
        else
        {
            _logger.LogError("Error parsing time from payload data.");
            return;
        }

        List<Task> tasks = new List<Task>();
        await _sensorService.SaveMeasurementsAsync(sensorId, timestamp, bsonElements);
    }

    private TopicSchema? MatchTopicSchema(string topic)
    {
        foreach (var schema in _topicSchemas)
        {
            if (MQTTnet.MqttTopicFilterComparer.Compare(topic, schema.TopicFilter) == MQTTnet.MqttTopicFilterCompareResult.IsMatch)
                return schema;
        }

        return null;
    }
}
