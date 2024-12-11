using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using MQTTnet.Server;
using SensorConsumer.Helpers;
using SensorConsumer.Configuration;
using SensorConsumer.Services;

namespace SensorConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttService _mqttService;
    private readonly MqttMessageProcessingService _processingService;

    private int _messageCount = 0;

    public Worker(ILogger<Worker> logger, MqttMessageProcessingService processingService, MqttService mqttService)
    {
        _logger = logger;
        _processingService = processingService;
        _mqttService = mqttService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker starting...");
        // Connects, subscribes to topics and keeps the connection alive
        await _mqttService.ConnectAsync(cancellationToken);
        // Handle incoming messages
        _mqttService.Client.ApplicationMessageReceivedAsync += ea => HandleReceivedMessage(cancellationToken, ea);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync called, shutting down...");
        await _mqttService.DisconnectAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleReceivedMessage(CancellationToken cancellationToken, MqttApplicationMessageReceivedEventArgs e)
    {
        e.AutoAcknowledge = false;
        int messageNumber = _messageCount++;

        async Task ProcessAsync()
        {
            using (_logger.BeginScope("Message number {messageNumber}, Topic: {topic}", messageNumber, e.ApplicationMessage.Topic))
            {
                try
                {
                    _logger.LogInformation("Received message. Message count: {messageNumber}", e.ApplicationMessage.Topic, messageNumber);

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("Message: {message};Payload: {payload}", e.ToJsonString(), e.ApplicationMessage.ConvertPayloadToString());

                    await _processingService.ProcessMessageAsync(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
                    await e.AcknowledgeAsync(cancellationToken);

                    _logger.LogInformation("Processed message number {messageNumber}.", messageNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message.");
                }
            }
        }

        _ = Task.Run(ProcessAsync, cancellationToken);
    }
}
