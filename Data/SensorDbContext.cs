using Microsoft.EntityFrameworkCore;
using MqttConsumer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttConsumer.Data;

public class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options)
    {
    }

    // DbSets for the models
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<SensorMeasurements> SensorMeasurements { get; set; }
}
