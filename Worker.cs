using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MqqtConsumer.Helpers;
using MqqtConsumer.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using MqqtConsumer.Services;

namespace MqqtConsumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MqttSettings _mqttSettings;
        private IMqttClient _mqttClient;
        private MqttFactory _mqttFactory;
        private int _messageCount = 0;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IOptions<MqttSettings> options)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _mqttSettings = options.Value;
            _mqttFactory = new MqttFactory();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting...");

            int retryCount = 0;
            int maxRetryDelay = 30; // Max delay in seconds

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_mqttClient.IsConnected)
                    {
                        await ConnectToBroker(cancellationToken);
                        await SubscribeToTopic(cancellationToken);

                        retryCount = 0;
                    }

                    _logger.LogInformation("Worker is alive and running. Messages counted so far: {messageCount}", _messageCount);

                    // Wait before checking again
                    await Task.Delay(5000, cancellationToken);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    // Calculate the backoff delay with exponential growth, limited to maxRetryDelay
                    int backoffDelay = Math.Min(1 << retryCount, maxRetryDelay);

                    _logger.LogError(ex, "An error occurred on retry attempt {retryCount}. Retrying in {backoffDelay} seconds", retryCount, backoffDelay);

                    if (retryCount > 10)
                        _logger.LogCritical(ex, "Maximum retry limit reached. Manual intervention might be required.");

                    // Wait for the backoff period or until cancellation is requested
                    await Task.Delay(TimeSpan.FromSeconds(backoffDelay), cancellationToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StopAsync called, shutting down...");

            try
            {
                if (_mqttClient.IsConnected)
                {
                    MqttClientDisconnectOptions mqttClientDisconnectOptions = _mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
                    await _mqttClient.DisconnectAsync(mqttClientDisconnectOptions);
                    _logger.LogInformation("Disconnected from MQTT broker.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while disconnecting.");
            }

            await base.StopAsync(cancellationToken);
        }

        private async Task ConnectToBroker(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connecting to MQTT broker: {broker}", _mqttSettings.Broker);

            // Parse the protocol version from the configuration string
            if (!Enum.TryParse(_mqttSettings.ProtocolVersion, out MqttProtocolVersion protocolVersion))
            {
                _logger.LogError("Invalid protocol version in configuration: {protocolVersion}", _mqttSettings.ProtocolVersion);
                protocolVersion = MqttProtocolVersion.Unknown;
            }

            MqttClientOptions options = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttSettings.Broker)
                .WithProtocolVersion(protocolVersion)
                .Build();

            MqttClientConnectResult response = await _mqttClient.ConnectAsync(options, cancellationToken);

            if (response.ResultCode == MqttClientConnectResultCode.Success)
            {
                _logger.LogInformation("Connected to MQTT broker");
            }
            else
            {
                _logger.LogError("Failed to connect to MQTT broker. ResultCode: {resultCode}, Reason: {reasonString}", response.ResultCode, response.ReasonString);
            }

            _logger.LogDebug("Response: {response}", response.ToJsonString());
        }

        private async Task SubscribeToTopic(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Subscribing to topic: {topicFilter}", _mqttSettings.TopicFilter);

            MqttClientSubscribeOptions mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(_mqttSettings.TopicFilter)
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions, cancellationToken);
            _logger.LogInformation("MQTT client subscribed to topic: {_mqttSettings.TopicFilter}", _mqttSettings.TopicFilter);
        }

        private async Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            _messageCount++;
            _logger.LogInformation("Received message on topic {topic}. Message: {message}. Message count: {_messageCount}", e.ApplicationMessage.Topic, e.ToJsonString(), _messageCount);

            _logger.LogInformation("Payload: {payload}", e.ApplicationMessage.ConvertPayloadToString());

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mqttMessageProcessingService = scope.ServiceProvider.GetRequiredService<MqttMessageProcessingService>();
                await mqttMessageProcessingService.ProcessMessageAsync(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
                _logger.LogInformation("Successfully processed message.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message.");
            }

            await Task.CompletedTask;

        }
    }
}
