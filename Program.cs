using Microsoft.Extensions.Options;
using SensorConsumer;
using SensorConsumer.Configuration;
using SensorConsumer.Configuration.Validators;
using SensorConsumer.Data;
using SensorConsumer.Services;
using SensorConsumer.Workers;

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

// Data
builder.Services.AddSingleton<MongoDb>();
builder.Services.AddSingleton<InactiveSensorCache>();

// Services
builder.Services.AddSingleton<MqttMessageProcessingService>();
builder.Services.AddSingleton<SensorService>();

// Workers
builder.Services.AddHostedService<InactiveSensorCacheWorker>();
builder.Services.AddHostedService<MqttWorker>(); // Main worker

var host = builder.Build();
host.Run();
