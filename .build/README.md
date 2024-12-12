# To run with docker compose

Build image in sensor-consumer project main directory.
```
docker build -t sensor-consumer-dev .
```
Modify the `compose.yaml` file to set used database and broker.
    - By default listens to [public aranet broker on hivemq](https://forum.aranet.com/aranet-bases-sensors/how-one-can-see-and-test-sample-of-mqtt-message-format-published-from-aranet-pro-base-station/).

Run the docker compose file in the main directory.
```
docker compose up -d
```
To see logs use docker desktop or run the following command.
```
docker logs sensor-consumer-dev
```
If everything is working correctly you should see new collections in the database with sensor data.

## If you want an image file 
Save image in tar file
```
docker save sensor-consumer-dev > sensor-consumer-dev.tar
```