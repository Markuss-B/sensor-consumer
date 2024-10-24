using Microsoft.EntityFrameworkCore;
using MqttConsumer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttConsumer.Data;

public static class Extensions
{
    public static void CreateDbIfNotExists(this IHost host)
    {
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<SensorDbContext>();
                context.Database.Migrate();
                //DbInitializer.Initialize(context);
            }
        }
    }
}