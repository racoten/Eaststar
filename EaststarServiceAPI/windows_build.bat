@echo off
REM Exit the script on any error
setlocal enabledelayedexpansion

REM Variables
set DOCKER_IMAGE_NAME=eaststar_service_api
set DOCKER_CONTAINER_NAME=eaststar_service_api_container
set BUILD_CONFIGURATION=Release

REM Build the project
echo Building the project...
dotnet build -c %BUILD_CONFIGURATION%
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

REM Publish the project
echo Publishing the project...
dotnet publish .\EaststarServiceAPI.csproj -c %BUILD_CONFIGURATION% -o .\publish
if %ERRORLEVEL% NEQ 0 (
    echo Publish failed!
    exit /b 1
)

REM Build the Docker image
echo Building the Docker image...
docker build -t %DOCKER_IMAGE_NAME% .
if %ERRORLEVEL% NEQ 0 (
    echo Docker build failed!
    exit /b 1
)

REM Stop and remove any existing container with the same name
for /f "tokens=*" %%i in ('docker ps -aq -f name=%DOCKER_CONTAINER_NAME%') do (
    echo Stopping and removing existing container...
    docker stop %%i
    docker rm %%i
)

REM Run the Docker container
echo Running the Docker container with network host...
docker run --network host --name %DOCKER_CONTAINER_NAME% -d %DOCKER_IMAGE_NAME%
if %ERRORLEVEL% NEQ 0 (
    echo Docker run failed!
    exit /b 1
)

REM Output container logs
echo Docker container logs:
docker logs -f %DOCKER_CONTAINER_NAME%
