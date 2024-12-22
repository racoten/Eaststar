#!/bin/bash

# Exit immediately if a command exits with a non-zero status
set -e

# Variables
DOCKER_IMAGE_NAME="eaststar_service_api"
DOCKER_CONTAINER_NAME="eaststar_service_api_container"
BUILD_CONFIGURATION="Release"

# Build the project
echo "Building the project..."
dotnet build -c $BUILD_CONFIGURATION

# Publish the project
echo "Publishing the project..."
dotnet publish ./EaststarServiceAPI.csproj -c $BUILD_CONFIGURATION -o ./publish

# Build the Docker image
echo "Building the Docker image..."
docker build -t $DOCKER_IMAGE_NAME .

# Stop and remove any existing container with the same name
if [ "$(docker ps -aq -f name=$DOCKER_CONTAINER_NAME)" ]; then
    echo "Stopping and removing existing container..."
    docker stop $DOCKER_CONTAINER_NAME
    docker rm $DOCKER_CONTAINER_NAME
fi

# Run the Docker container
echo "Running the Docker container with network host..."
docker run --network host --name $DOCKER_CONTAINER_NAME -d $DOCKER_IMAGE_NAME

# Output container logs
echo "Docker container logs:"
docker logs -f $DOCKER_CONTAINER_NAME
