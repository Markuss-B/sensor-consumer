using MQTTnet.Client;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SensorConsumer.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace SensorConsumer.Services;

public class MqttService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttClient _client;

    private readonly MqttClientOptions _options;
    private readonly MqttFactory _factory;
    private List<string> _topicsToSubscribeTo { get; set; }

    public IMqttClient Client => _client;

    public MqttService(ILogger<MqttService> logger, IOptions<MqttSettings> options)
    {
        _logger = logger;
        _factory = new MqttFactory();
        _options = ConfigureOptions(options.Value);
        _client = _factory.CreateMqttClient();
        _topicsToSubscribeTo = options.Value.TopicFilter.Split(',').ToList();
    }

    /// <summary>
    /// Connects to the MQTT broker and keeps the connection alive. Also subscribes to topics specified in the TopicsToSubscribeTo property.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>True once an initial connection has been made. Returns false if connection is cancelled before an initial connection has been made.</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _client.ConnectAsync(_options, cancellationToken);
                _logger.LogInformation("Connected to MQTT broker");
                await SubscribeToTopics();
                _logger.LogInformation("Subscribed to topics");

                KeepConnection(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while connecting to the MQTT broker. Retrying..");
                await Task.Delay(5000, cancellationToken);
            }
        }

        return false;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _client.DisconnectAsync();
        _logger.LogInformation("Disconnected from MQTT broker.");
    }

    /// <summary>
    /// Keep the connection alive by pinging the broker every 5 seconds.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task KeepConnection(CancellationToken cancellationToken)
    {
        int retryCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await _client.TryPingAsync())
                {
                    await _client.ConnectAsync(_options, cancellationToken);
                    _logger.LogInformation("Connected to MQTT broker.");
                    await SubscribeToTopics();
                    _logger.LogInformation("Subscribed to topics.");

                    retryCount = 0;
                }

                _logger.LogInformation("MQTT connection is alive.");
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

    /// <summary>
    /// Subscribe to the topics specified in the TopicsToSubscribeTo property.
    /// </summary>
    private async Task SubscribeToTopics()
    {
        if (_topicsToSubscribeTo == null)
        {
            _logger.LogWarning("No topics to subscribe to.");
            return;
        }
        foreach (var topic in _topicsToSubscribeTo)
        {
            await _client.SubscribeAsync(topic);
            _logger.LogDebug("Subscribed to topic: {topic}", topic);
        }
    }

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
