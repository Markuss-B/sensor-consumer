using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MqttConsumer.Helpers;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using MqttConsumer.Services;
using MqttConsumer.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using MQTTnet.Server;

namespace MqttConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttSettings _mqttSettings;
    private readonly MqttMessageProcessingService _processingService;
    private readonly MqttClientOptions _mqttClientOptions;

    private IMqttClient _mqttClient;
    private MqttFactory _mqttFactory;
    private int _messageCount = 0;

    public Worker(ILogger<Worker> logger, MqttMessageProcessingService processingService, IOptions<MqttSettings> options)
    {
        _logger = logger;
        _processingService = processingService;
        _mqttSettings = options.Value;
        _mqttFactory = new MqttFactory();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttSettings.Broker)
            .WithProtocolVersion(_mqttSettings.ProtocolVersion)
            .WithCredentials(_mqttSettings.Username, _mqttSettings.Password);
        if (_mqttSettings.UseTls)
        {
            X509Certificate2Collection x509Certificate2s = new();
            x509Certificate2s.ImportFromPemFile(_mqttSettings.PathToPem);

            optionsBuilder.WithTlsOptions(new MqttClientTlsOptions()
            {
                UseTls = true,
                TrustChain = x509Certificate2s,
                SslProtocol = _mqttSettings.SslProtocol,
                CertificateValidationHandler = CertificateValidationHandler
            });
        }

        _mqttClientOptions = optionsBuilder.Build();
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

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await _mqttClient.TryPingAsync())
                {
                    await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);
                    _logger.LogInformation("Connected to MQTT broker");

                    await SubscribeToTopic(cancellationToken);

                    retryCount = 0;
                }

                _logger.LogInformation("Worker is alive and running. Messages counted so far: {messageCount}", _messageCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred on retry attempt {retryCount}. Retrying..", retryCount);

                if (retryCount > 10)
                    _logger.LogCritical(ex, "Maximum retry limit reached. Manual intervention might be required.");

                retryCount++;
            }
            finally
            {
                await Task.Delay(5000, cancellationToken);
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

    private bool CertificateValidationHandler(MqttClientCertificateValidationEventArgs eventArgs)
    {
        _logger.LogError("Certificate Validation Handler called.");

        _logger.LogError("Certificate Subject: {subject}", eventArgs.Certificate.Subject.ToJsonString());
        _logger.LogError("Certificate Expiration Date: {expirationDate}", eventArgs.Certificate.GetExpirationDateString().ToJsonString());
        _logger.LogError("Chain Revocation Mode: {revocationMode}", eventArgs.Chain.ChainPolicy.RevocationMode.ToJsonString());
        _logger.LogError("Chain Status: {chainStatus}", eventArgs.Chain.ChainStatus.ToJsonString());
        _logger.LogError("SSL Policy Errors: {sslPolicyErrors}", eventArgs.SslPolicyErrors.ToJsonString());

        return true;
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
            await _processingService.ProcessMessageAsync(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());

            _logger.LogInformation("Successfully processed message.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message.");
        }

        await Task.CompletedTask;

    }
}
