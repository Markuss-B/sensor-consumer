using Microsoft.Extensions.Options;
using SensorMonitoring.Consumer;
using SensorMonitoring.Consumer.Configuration;
using SensorMonitoring.Consumer.Configuration.Validators;
using SensorMonitoring.Consumer.Data;
using SensorMonitoring.Consumer.Services;
using SensorMonitoring.Consumer.Workers;

var builder = Host.CreateApplicationBuilder(args);

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MqttSettings"));
builder.Services.AddSingleton<IValidateOptions<MqttSettings>, MqttSettingsValidation>();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<MongoDb>();
builder.Services.AddSingleton<InactiveSensorCache>();

builder.Services.AddSingleton<MqttMessageProcessingService>();
builder.Services.AddSingleton<SensorService>();

builder.Services.AddHostedService<InactiveSensorCacheWorker>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
