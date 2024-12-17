using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using SensorConsumer.Configuration;
using SensorConsumer.Helpers;
using SensorConsumer.Services;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SensorConsumer.Workers;

/// <summary>
/// Worker service that connects to an MQTT broker, subscribes to topics and routes incoming messages to a processing service.
/// </summary>
public class MqttWorker : BackgroundService
{
    private readonly ILogger<MqttWorker> _logger;
    private readonly IMqttClient _client;

    private readonly MqttSettings _settings;
    private readonly MqttClientOptions _options;

    private readonly MqttMessageProcessingService _processingService;
    private int _messageCount = 0;

    public MqttWorker(ILogger<MqttWorker> logger, IOptions<MqttSettings> options, MqttMessageProcessingService processingService)
    {
        _logger = logger;
        _settings = options.Value;
        _options = ConfigureOptions(_settings);
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
        _processingService = processingService;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Starts receiving messages and keep the connection alive by pinging the broker every 5 seconds.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Handle incoming messages
        _client.ApplicationMessageReceivedAsync += ea => HandleReceivedMessage(cancellationToken, ea);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await _client.TryPingAsync(cancellationToken))
                {
                    _logger.LogWarning("MQTT connection is not alive. Attempting to reconnect.");
                    await Connect(cancellationToken);
                }

                _logger.LogInformation("MQTT connection is alive.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred on ping attempt.");
            }

            // Check connection every 5 seconds
            await Task.Delay(5000, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync called, shutting down...");
        await _client.DisconnectAsync();

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

    /// <summary>
    /// Connect to the MQTT broker and subscribe to topics.
    /// </summary>
    private async Task Connect(CancellationToken cancellationToken)
    {
        int retryCount = 0;

        while (!_client.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Connecting to MQTT broker: {broker}", _settings.Broker);

                await _client.ConnectAsync(_options, cancellationToken);
                _logger.LogInformation("Connected to MQTT broker");

                await SubscribeToTopics();
                _logger.LogInformation("Subscribed to topics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred on connect attempt {retryCount}.", retryCount);
                retryCount++;
                // Wait for a maximum of 30 seconds before retrying
                await Task.Delay(Math.Min(1000 * retryCount, 30000), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Subscribe to the topics specified in the TopicsToSubscribeTo property.
    /// </summary>
    private async Task SubscribeToTopics()
    {
        string[] topics = _settings.TopicSchemas.Select(s => s.TopicFilter).ToArray();

        if (topics.Length == 0)
        {
            _logger.LogWarning("No topics to subscribe to.");
            return;
        }
        _logger.LogDebug("Subscribing to topics: {topics}.", topics);
        foreach (var topic in topics)
        {
            await _client.SubscribeAsync(topic);
            _logger.LogDebug("Subscribed to topic: {topic}", topic);
        }
    }

    /// <summary>
    /// Configure the options for the MQTT client based on the settings.
    /// </summary>
    private MqttClientOptions ConfigureOptions(MqttSettings settings)
    {
        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.Broker)
            .WithProtocolVersion(settings.ProtocolVersion)
            .WithCredentials(settings.Username, settings.Password);

        if (settings.UseTls)
        {
            X509Certificate2Collection certificates = new();
            certificates.ImportFromPemFile(settings.PathToPem);

            optionsBuilder.WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                TrustChain = certificates,
                SslProtocol = settings.SslProtocol,
                CertificateValidationHandler = CertificateValidationHandler
            });
        }

        return optionsBuilder.Build();
    }

    /// <summary>
    /// Logs information about the certificate validation.
    /// </summary>
    private bool CertificateValidationHandler(MqttClientCertificateValidationEventArgs eventArgs)
    {
        _logger.LogInformation("Certificate Validation Handler called.");

        _logger.LogInformation("Certificate Subject: {subject}", eventArgs.Certificate.Subject);
        _logger.LogInformation("Certificate Expiration Date: {expirationDate}", eventArgs.Certificate.GetExpirationDateString());
        _logger.LogInformation("Chain Revocation Mode: {revocationMode}", eventArgs.Chain.ChainPolicy.RevocationMode);
        _logger.LogInformation("Chain Status: {chainStatus}", eventArgs.Chain.ChainStatus);
        if (eventArgs.SslPolicyErrors != SslPolicyErrors.None)
            _logger.LogWarning("SSL Policy Errors: {sslPolicyErrors}", eventArgs.SslPolicyErrors);

        return true;
    }
}
