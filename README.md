# MqqtConsumer

# Setup

## Docker setup
1. Create network
```
docker network create mynetwork
```

2. Create db
https://www.mongodb.com/docs/manual/tutorial/install-mongodb-community-with-docker/
```
docker pull mongodb/mongodb-community-server:latest
docker run --name mongodb --network mynetwork -p 27017:27017 -d mongodb/mongodb-community-server:latest
```

3. Create app image and run container
In project folder
Specify connection string in .env file and run commands
```
docker build -t mqtt-aranet-sensor-consumer .
docker run -d --env-file .env --name mqtt-aranet-sensor-consumer --network mynetwork mqtt-aranet-sensor-consumer
```
or just build the image and specify variables in run command

to limit resources
```
docker run -d --env-file .env --name mqtt-aranet-sensor-consumer-limited-resources --network mynetwork --memory="256m" --cpus="0.5" mqtt-aranet-sensor-consumer
```