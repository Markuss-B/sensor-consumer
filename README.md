## Part of Sensor Monitoring System
- [sensor-consumer](https://github.com/Markuss-B/sensor-consumer)
- [sensor-webapi](https://github.com/Markuss-B/sensor-webapi)
- [sensor-dashboard](https://github.com/Markuss-B/sensor-dashboard)
- [sensor-notifications](https://github.com/Markuss-B/sensor-notifications)
- [sensor-courier](https://github.com/Markuss-B/sensor-courier)

## Local Docker setup
Check [.build folder](.build/)

# MQTT consumer for LU Aranet sensor data.

# Features
- Listens to MQTT broker for sensor data and saves it to MongoDB.
- Configurable connection settings in appsettings for MQTT broker and MongoDB.
- Configurable topics in appsettings. Ex. 'Aranet/+/sensors/<sensorId>/json/measurements'.
- Async message processing.
- Extracts 'time' field from JSON and converts it to datetime to use as timestamp for measurement timeseries.
- Identifies Aranet metadata topics with cinfigured topic 'Aranet/+/sensors/<sensorId>/<metadataName>'.
- Saves metadata to actual sensor and metadata history collections.
- Tracable message logs by message log context.
- Docker container setup.

## Web integration by sensor-webapi and sensor-dashboard
- Ignores inactive sensors marked by 'isActive' field which is managed by web project.
- Keeps a cache of ignored sensors which is tracked by mongo change streams.
