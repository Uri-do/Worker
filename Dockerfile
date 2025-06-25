# Multi-stage Dockerfile for MonitoringWorker
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["MonitoringWorker.csproj", "."]
RUN dotnet restore "MonitoringWorker.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src"
RUN dotnet build "MonitoringWorker.csproj" -c Release -o /app/build

# Run tests
FROM build AS test
WORKDIR /src
COPY ["Tests/MonitoringWorker.Tests.csproj", "Tests/"]
RUN dotnet restore "Tests/MonitoringWorker.Tests.csproj"
COPY Tests/ Tests/
RUN dotnet test "Tests/MonitoringWorker.Tests.csproj" --configuration Release --no-restore --verbosity normal

# Publish application
FROM build AS publish
RUN dotnet publish "MonitoringWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/healthz/live || exit 1

# Labels for metadata
LABEL maintainer="MonitoringWorker Team"
LABEL version="1.0"
LABEL description="Scheduled Monitoring Service with SignalR notifications"

ENTRYPOINT ["dotnet", "MonitoringWorker.dll"]
