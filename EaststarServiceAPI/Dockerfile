# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 0-65535

# Install OpenSSL and generate self-signed certificates for localhost
RUN apt-get update && apt-get install -y openssl && \
    mkdir -p /etc/letsencrypt/live/localhost && \
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /etc/letsencrypt/live/localhost/privkey.pem \
        -out /etc/letsencrypt/live/localhost/fullchain.pem \
        -subj "/CN=localhost"
USER $APP_UID

# Build image for development
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EaststarServiceAPI.csproj", "EaststarServiceAPI/"]
RUN dotnet restore "./EaststarServiceAPI/EaststarServiceAPI.csproj"
COPY . .
WORKDIR "/src/EaststarServiceAPI"
RUN dotnet build "./EaststarServiceAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EaststarServiceAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage for runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EaststarServiceAPI.dll"]