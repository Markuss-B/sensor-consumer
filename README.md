# MQTT consumer for LU Aranet sensor data.

# Features
- Listens to MQTT broker for sensor data and saves it to MongoDB.
- Configurable connection settings in appsettings for MQTT broker and MongoDB.
- Configurable topics in appsettings. Ex. 'Aranet/#'.
- Async message processing.
- Identifies sensor measurements in JSON format on topics ending with '/measurements/'. Ex. 'Aranet/<baseSationId>/sensors/<sensorId>/json/measurements/'
- Extracts 'time' field from JSON and converts it to datetime to use as timestamp for measurement timeseries.
- Identifies Aranet metadata topics like 'Aranet/<baseSationId>/sensors/<sensorId>/name', 'group' and saves metadata to database.
- Saves metadata to actual sensor and metadata history collections.
- Tracable logs by message log context.
- Docker container setup.

## Web integration by sensor-webapi and sensor-webapp
- Ignores inactive sensors marked by 'isActive' field which is managed by web project.
- Keeps a cache of ignored sensors which is tracked by mongo change streams.

# Local Docker setup
Check [.build folder](.build/)
