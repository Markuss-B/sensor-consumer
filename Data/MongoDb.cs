using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MqttConsumer.Configuration;
using MqttConsumer.Models;

namespace MqttConsumer.Data;

public class MongoDb
{
    public MongoDb(IOptions<MongoDbSettings> options)
    {
        MongoDbSettings settings = options.Value;
        MongoClient client = new MongoClient(settings.ConnectionString);
        IMongoDatabase database = client.GetDatabase(settings.DatabaseName);

        sensors = database.GetCollection<Sensor>("sensors");
        sensorMeasurements = database.GetCollection<SensorMeasurements>("sensorMeasurements");
        sensorMetadatas = database.GetCollection<SensorMetadatas>("sensorMetadatas");
    }

    public IMongoCollection<Sensor> sensors { get; set; }
    public IMongoCollection<SensorMeasurements> sensorMeasurements { get; set; }
    public IMongoCollection<SensorMetadatas> sensorMetadatas { get; set; }
}
