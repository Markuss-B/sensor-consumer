using Microsoft.Extensions.Options;
using MqttConsumer;
using MqttConsumer.Configuration;
using MqttConsumer.Configuration.Validators;
using MqttConsumer.Data;
using MqttConsumer.Services;

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

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddSingleton<MqttMessageProcessingService>();
builder.Services.AddSingleton<SensorService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
