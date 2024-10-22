using Microsoft.EntityFrameworkCore;
using MqqtConsumer;
using MqqtConsumer.Data;
using MqqtConsumer.Models;
using MqqtConsumer.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MqttSettings"));

builder.Services.AddDbContext<SensorDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SensorDb"));
});

builder.Services.AddScoped<MqttMessageProcessingService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
