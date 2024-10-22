using Microsoft.EntityFrameworkCore;
using MqqtConsumer.Data;
using MqqtConsumer.Models;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MqqtConsumer.Services;

public class MqttMessageProcessingService
{
    private readonly ILogger<MqttMessageProcessingService> _logger;
    private readonly SensorDbContext _context;

    public MqttMessageProcessingService(ILogger<MqttMessageProcessingService> logger, SensorDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    // Main method that receives the message and delegates to the appropriate handler
    public async Task ProcessMessageAsync(string topic, string payload)
    {
        // Extract the last part of the topic to identify the payload type
        var topicParts = topic.Split('/');
        var payloadType = topicParts.Last();

        await HandleTopic(topic);

        // Use a switch expression to route to the appropriate handler
        var task = payloadType switch
        {
            "measurements" => HandleMeasurementsAsync(topic, payload),
            "name" => HandleSensorMetadataAsync(topic, payload, "Name"),
            "productNumber" => HandleSensorMetadataAsync(topic, payload, "ProductNumber"),
            "group" => HandleSensorMetadataAsync(topic, payload, "Group"),
            "groupId" => HandleSensorMetadataAsync(topic, payload, "GroupId"),
            _ => throw new InvalidOperationException($"Unknown topic: {topic}")
        };

        await task;
    }

    private async Task HandleTopic(string topic)
    {
        var topicParts = topic.Split('/');
        if (topicParts.Length < 4)
        {
            throw new InvalidOperationException($"Invalid topic: {topic}");
        }

        var rootTopic = topicParts[0];
        var baseSerialNumber = topicParts[1];
        var sensorId = topicParts[3];

        Sensor sensor = await _context.Sensors.FindAsync(sensorId);

        if (sensor == null)
        {
            sensor = new Sensor
            {
                Id = sensorId,
                BaseSerialNumber = baseSerialNumber,
                RootTopic = rootTopic
            };

            _context.Sensors.Add(sensor);
        }
        else
        {
            sensor.BaseSerialNumber = baseSerialNumber;
            sensor.RootTopic = rootTopic;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Handles measurments. Topics like: Aranetest/394260700033/sensors/3002FA/json/measurements
    /// </summary>
    /// <param name="topic">Measurment topic</param>
    /// <param name="payload">Measurment payload - json string containing measurments like co2, temperature, battery.</param>
    /// <returns></returns>
    private async Task HandleMeasurementsAsync(string topic, string payload)
    {
        string sensorId = ExtractSensorIdFromTopic(topic);
        SensorMeasurementsPayload sensorData;

        try
        {
            sensorData = JsonSerializer.Deserialize<SensorMeasurementsPayload>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize sensor measurements payload for sensor {sensorId}. Payload: {payload}", sensorId, payload);
            return;
        }

        var measurement = new SensorMeasurements
        {
            Timestamp = sensorData.Time,
            SensorId = sensorId,
            CO2 = sensorData.CO2,
            Temperature = sensorData.Temperature,
            Battery = sensorData.Battery,
            AtmosphericPressure = sensorData.AtmosphericPressure,
            Rssi = sensorData.Rssi
        };

        _context.SensorMeasurements.Add(measurement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully saved measurements for sensor {sensorId}", sensorId);
    }

    // Generic handler for sensor metadata
    private async Task HandleSensorMetadataAsync(string topic, string payload, string metadataType)
    {
        var sensorId = ExtractSensorIdFromTopic(topic);

        // Fetch or create sensor
        var sensor = await _context.Sensors.FindAsync(sensorId);

        // Assign the correct metadata field based on metadataType
        _ = metadataType switch
        {
            "Name" => sensor.Name = payload,
            "ProductNumber" => sensor.ProductNumber = payload,
            "Group" => sensor.Group = payload,
            "GroupId" => sensor.GroupId = payload,
            _ => throw new NotSupportedException($"Metadata type {metadataType} is not supported")
        };

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully saved {metadataType} for sensor {sensorId}", metadataType, sensorId);
    }

    // Helper method to extract sensor ID from the topic
    private string ExtractSensorIdFromTopic(string topic)
    {
        var topicParts = topic.Split('/');
        return topicParts[3]; // Assuming sensor ID is always at this position in the topic
    }
}
