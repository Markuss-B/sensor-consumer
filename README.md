# MqqtConsumer

# Setup

## Docker setup
1. Create network
```
docker network create mynetwork
```

2. Create db
```
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=[yourpassword]" -p 1433:1433 --name sql1 --hostname sql1 --network mynetwork -d mcr.microsoft.com/mssql/server:2022-latest
```
https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker
2.1. Create db user

3. Create app image and run container
In project folder
Specify connection string in .env file and run commands
```
docker build -t mqtt-aranet-sensor-consumer .
docker run -d --env-file .env --name mqtt-aranet-sensor-consumer --network mynetwork mqtt-aranet-sensor-consumer
```
or just build the image and specify variables in run command