# MonitoringWorker - .NET 8 Scheduled Monitoring Service

A comprehensive, production-ready worker service built with .NET 8 that performs scheduled monitoring of HTTP endpoints and SQL Server databases, with JWT authentication, real-time status updates via SignalR, and enterprise-grade configuration management.

## 🚀 Features

### Core Monitoring
- **HTTP Endpoint Monitoring**: Configurable health checks for web services with custom status codes and timeouts
- **SQL Server Database Monitoring**: Execute custom queries, connection health checks, and performance monitoring
- **Scheduled Execution**: Uses Quartz.NET for cron-style scheduling with configurable intervals
- **Parallel Processing**: Concurrent monitoring of multiple endpoints and databases

### Security & Authentication
- **JWT Authentication**: Role-based access control with Admin, Operator, and Viewer roles
- **Permission-based Authorization**: Fine-grained permissions for different operations
- **Secure SignalR**: Authenticated real-time connections with user context
- **API Security**: Protected REST endpoints with JWT bearer tokens

### Real-time & Communication
- **SignalR Integration**: WebSocket-based real-time notifications to connected clients
- **User-aware Notifications**: Context-aware messaging based on user roles and permissions
- **REST API**: Comprehensive API for configuration, monitoring, and database operations
- **Interactive Dashboard**: Web-based monitoring dashboard with authentication

### Resilience & Reliability
- **Polly Integration**: Retry policies and circuit breakers for HTTP requests and database connections
- **Connection Pooling**: Efficient database connection management with configurable limits
- **Graceful Degradation**: Continues operation even when individual components fail
- **Health Checks**: Separate liveness and readiness probes for container orchestration

### Configuration & Management
- **Advanced Configuration Service**: Hot reload, validation, and change tracking
- **Environment-specific Settings**: Development, staging, and production configurations
- **Configuration API**: Runtime configuration updates with validation
- **Change History**: Track all configuration changes with timestamps and user attribution

### Observability & Monitoring
- **Structured Logging**: Serilog with enrichers for environment, thread, and correlation IDs
- **Comprehensive Metrics**: Job execution, check results, and performance metrics
- **Database Statistics**: Connection health, query performance, and environment grouping
- **Error Tracking**: Detailed error logging with context and stack traces

### Enterprise Features
- **Containerization**: Docker support with multi-stage builds and security best practices
- **Dependency Injection**: Full DI container integration with modern .NET 8 patterns
- **Comprehensive Testing**: 113 unit tests with high coverage and integration testing
- **Production Ready**: Enterprise-grade error handling, logging, and configuration management

## 📁 Project Structure

```
MonitoringWorker/
├── Configuration/
│   ├── AuthenticationOptions.cs     # JWT and user authentication configuration
│   ├── DatabaseMonitoringOptions.cs # Database monitoring configuration
│   ├── MonitoringOptions.cs         # HTTP endpoint monitoring configuration
│   └── QuartzOptions.cs             # Quartz scheduling configuration
├── Controllers/
│   ├── AuthController.cs            # Authentication and user management API
│   ├── ConfigurationController.cs   # Configuration management API
│   ├── DatabaseMonitoringController.cs # Database monitoring API
│   └── MonitoringController.cs      # HTTP endpoint monitoring API
├── HealthChecks/
│   ├── LivenessHealthCheck.cs       # Basic liveness probe
│   └── ReadinessHealthCheck.cs      # Readiness check with dependency validation
├── Hubs/
│   └── MonitoringHub.cs             # SignalR hub with authentication
├── Jobs/
│   └── MonitoringJob.cs             # Quartz job implementation
├── Models/
│   ├── AuthModels.cs                # Authentication models and permissions
│   ├── DatabaseMonitoringModels.cs  # Database monitoring models
│   ├── MonitoringEvent.cs           # Event model for SignalR notifications
│   ├── MonitoringResult.cs          # Result model for monitoring checks
│   └── MonitoringStatus.cs          # Status enumeration
├── Services/
│   ├── AuthenticationService.cs     # JWT authentication service
│   ├── ConfigurationService.cs      # Advanced configuration management
│   ├── DatabaseMonitoringService.cs # SQL Server monitoring service
│   ├── EventNotificationService.cs  # SignalR notification service
│   ├── IAuthenticationService.cs    # Authentication service interface
│   ├── IConfigurationService.cs     # Configuration service interface
│   ├── IDatabaseMonitoringService.cs # Database monitoring interface
│   ├── IEventNotificationService.cs # Notification service interface
│   ├── IMetricsService.cs           # Metrics service interface
│   ├── IMonitoringService.cs        # Monitoring service interface
│   ├── MetricsService.cs            # Metrics collection and aggregation
│   └── MonitoringService.cs         # Core monitoring logic with HTTP checks
├── Tests/
│   ├── AuthenticationServiceTests.cs    # Authentication service tests
│   ├── ConfigurationServiceTests.cs     # Configuration service tests
│   ├── DatabaseMonitoringServiceTests.cs # Database monitoring tests
│   ├── Jobs/
│   │   └── MonitoringJobTests.cs        # Unit tests for monitoring job
│   ├── Services/
│   │   ├── MetricsServiceTests.cs       # Unit tests for metrics service
│   │   └── MonitoringServiceTests.cs    # Unit tests for monitoring service
│   └── MonitoringWorker.Tests.csproj    # Test project file (122 tests total)
├── wwwroot/
│   └── index.html                   # Real-time monitoring dashboard
├── Program.cs                       # Application entry point with DI setup
├── Worker.cs                        # Background service for heartbeat
├── appsettings.json                 # Production configuration
├── appsettings.Development.json     # Development configuration
├── Dockerfile                       # Multi-stage container definition
├── docker-compose.yml               # Container orchestration
├── docker-compose.override.yml      # Development overrides
├── nginx.conf                       # Reverse proxy configuration
├── MonitoringWorker.sln             # Visual Studio solution file
├── .gitignore                       # Git ignore patterns
└── MonitoringWorker.csproj          # Project file with dependencies
```

## 🛠️ Getting Started

### Prerequisites

- .NET 8 SDK
- Docker (optional, for containerization)
- Visual Studio 2022 or VS Code (recommended)
- SQL Server (for database monitoring features)

### Running Locally

1. **Clone and navigate to the project directory**

2. **Open the solution**:
   ```bash
   # Using Visual Studio
   start MonitoringWorker.sln

   # Using VS Code
   code .

   # Using command line
   dotnet build MonitoringWorker.sln
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore MonitoringWorker.sln
   ```

4. **Run the application**:
   ```bash
   # Run from solution
   dotnet run --project MonitoringWorker.csproj

   # Or run directly
   dotnet run
   ```

5. **Run tests**:
   ```bash
   # Run all tests in solution
   dotnet test MonitoringWorker.sln

   # Run with coverage
   dotnet test --collect:"XPlat Code Coverage"
   ```

6. **Access the dashboard**: Open http://localhost:5000 in your browser

### Running with Docker

```bash
# Build and run with docker-compose
docker-compose up --build

# Or build the image manually
docker build -t monitoring-worker .
docker run -p 5000:5000 monitoring-worker
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test Tests/MonitoringWorker.Tests.csproj
```

## ⚙️ Configuration

### appsettings.json

```json
{
  "Monitoring": {
    "DefaultTimeoutSeconds": 30,
    "Endpoints": [
      {
        "Name": "HealthCheck",
        "Url": "https://api.example.com/health",
        "TimeoutSeconds": 10,
        "Method": "GET",
        "ExpectedStatusCodes": [200],
        "Headers": {
          "Authorization": "Bearer token"
        }
      }
    ]
  },
  "Quartz": {
    "CronSchedule": "0 0/1 * * * ?",
    "MaxConcurrentJobs": 1,
    "WaitForJobsToComplete": true
  }
}
```

### Environment Variables

```bash
# Override configuration via environment variables
Monitoring__DefaultTimeoutSeconds=60
Monitoring__Endpoints__0__Url=https://api.production.com/health
Quartz__CronSchedule="0 0/5 * * * ?"
```

### Configuration Validation

The application uses data annotations for configuration validation:
- Endpoint URLs must be valid
- Timeout values must be between 1-300 seconds
- At least one endpoint must be configured
- Cron schedule is required

## 🔐 Authentication & Security

### JWT Authentication

The service supports JWT-based authentication with role-based access control:

```json
{
  "Authentication": {
    "Enabled": true,
    "Jwt": {
      "SecretKey": "your-super-secret-jwt-key-that-is-at-least-32-characters-long",
      "Issuer": "MonitoringWorker",
      "Audience": "MonitoringWorker",
      "ExpirationMinutes": 60,
      "ClockSkewMinutes": 5
    },
    "Users": {
      "DefaultUsers": [
        {
          "Id": "admin-001",
          "Username": "admin",
          "Password": "admin123",
          "Roles": ["Admin"],
          "Enabled": true
        }
      ]
    }
  }
}
```

### User Roles & Permissions

| Role | Permissions |
|------|-------------|
| **Admin** | Full access: view/manage monitoring, configuration, users, metrics, logs |
| **Operator** | Monitor management: view/manage monitoring, view metrics and logs |
| **Viewer** | Read-only: view monitoring and metrics only |

### API Authentication

```bash
# Login to get JWT token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'

# Use token in subsequent requests
curl -X GET http://localhost:5000/api/configuration \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🗄️ Database Monitoring

### SQL Server Configuration

Configure database connections and monitoring queries:

```json
{
  "DatabaseMonitoring": {
    "Enabled": true,
    "DefaultConnectionTimeoutSeconds": 30,
    "DefaultCommandTimeoutSeconds": 30,
    "MaxConcurrentConnections": 10,
    "Connections": [
      {
        "Name": "ProductionDB",
        "ConnectionString": "Server=sql-server;Database=MyApp;Trusted_Connection=true;",
        "Provider": "SqlServer",
        "Environment": "Production",
        "Enabled": true,
        "Tags": ["critical", "production"],
        "QueryNames": ["HealthCheck", "Performance"]
      }
    ],
    "Queries": [
      {
        "Name": "HealthCheck",
        "Sql": "SELECT 1 as HealthCheck",
        "Description": "Basic database connectivity check",
        "Type": "HealthCheck",
        "ResultType": "Scalar",
        "ExpectedValue": 1,
        "ComparisonOperator": "Equals",
        "Enabled": true
      },
      {
        "Name": "Performance",
        "Sql": "SELECT COUNT(*) FROM sys.dm_exec_requests WHERE status = 'running'",
        "Description": "Active connections count",
        "Type": "Performance",
        "ResultType": "Scalar",
        "WarningThreshold": 50,
        "CriticalThreshold": 100,
        "Enabled": true
      }
    ]
  }
}
```

### Database API Endpoints

```bash
# Test all database connections
curl -X GET http://localhost:5000/api/database-monitoring/connections/test \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Execute monitoring queries
curl -X GET http://localhost:5000/api/database-monitoring/queries/execute \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Execute custom query
curl -X POST http://localhost:5000/api/database-monitoring/connections/ProductionDB/custom-query \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sql": "SELECT COUNT(*) FROM Users", "timeoutSeconds": 30}'

# Get database statistics
curl -X GET http://localhost:5000/api/database-monitoring/statistics \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🔄 Scheduling

### Cron Expressions

Modify the cron expression in configuration:

```json
{
  "Quartz": {
    "CronSchedule": "0 */5 * * * ?"
  }
}
```

**Common cron expressions:**
- `0 0/30 * * * ?` - Every 30 minutes
- `0 0 * * * ?` - Every hour
- `0 0 0 * * ?` - Daily at midnight
- `0 0 8 ? * MON-FRI` - Weekdays at 8 AM
- `0/30 * * * * ?` - Every 30 seconds

## 🌐 SignalR Client Examples

### JavaScript Client with Authentication

```javascript
// First, authenticate and get JWT token
async function authenticate() {
    const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: 'admin', password: 'admin123' })
    });
    const data = await response.json();
    localStorage.setItem('jwt_token', data.accessToken);
    return data.accessToken;
}

// Create SignalR connection with authentication
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/monitoringHub", {
        accessTokenFactory: () => localStorage.getItem("jwt_token")
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Handle connection events
connection.on("Connected", (data) => {
    console.log("Connected:", data);
    console.log("User roles:", data.Roles);
    console.log("User permissions:", data.Permissions);
});

// Handle monitoring updates
connection.on("MonitoringUpdate", (event) => {
    console.log("Monitoring Update:", event);
    // Update UI with event data
});

// Handle database monitoring updates
connection.on("DatabaseMonitoringUpdate", (event) => {
    console.log("Database Update:", event);
    // Update database status in UI
});

// Start connection
async function start() {
    try {
        await authenticate(); // Get JWT token first
        await connection.start();
        console.log("SignalR Connected");

        // Get user info
        await connection.invoke("GetUserInfo");

        // Request current metrics (requires ViewMetrics permission)
        await connection.invoke("GetMetrics");
    } catch (err) {
        console.error(err);
        setTimeout(start, 5000);
    }
}

start();
```

### .NET Client with Authentication

```csharp
using Microsoft.AspNetCore.SignalR.Client;

// Authenticate first
var httpClient = new HttpClient();
var loginResponse = await httpClient.PostAsJsonAsync("http://localhost:5000/api/auth/login",
    new { username = "admin", password = "admin123" });
var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

// Create connection with JWT token
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/monitoringHub", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(loginData.AccessToken);
    })
    .Build();

// Handle events
connection.On<object>("Connected", (data) =>
{
    Console.WriteLine($"Connected: {data}");
});

connection.On<MonitoringEvent>("MonitoringUpdate", (monitoringEvent) =>
{
    Console.WriteLine($"Monitoring Update: {monitoringEvent.CheckName} - {monitoringEvent.Status}");
});

connection.On<DatabaseMonitoringResult>("DatabaseMonitoringUpdate", (result) =>
{
    Console.WriteLine($"Database Update: {result.ConnectionName}.{result.QueryName} - {result.Status}");
});

await connection.StartAsync();

// Get user information
await connection.InvokeAsync("GetUserInfo");
```

## 🔌 API Reference

### Authentication Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | Authenticate user and get JWT token | No |
| GET | `/api/auth/me` | Get current user information | Yes |
| POST | `/api/auth/validate` | Validate current JWT token | Yes |
| GET | `/api/auth/check-permission?permission={perm}` | Check if user has specific permission | Yes |
| GET | `/api/auth/permissions` | Get available permissions (Admin only) | Yes (Admin) |

### HTTP Endpoint Monitoring

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/monitoring/check` | Trigger manual check for all endpoints | Yes (ManageMonitoring) |
| POST | `/api/monitoring/check/{endpointName}` | Trigger manual check for specific endpoint | Yes (ManageMonitoring) |
| GET | `/api/monitoring/metrics` | Get current monitoring metrics | Yes (ViewMetrics) |
| GET | `/api/monitoring/status` | Get monitoring status summary | Yes (ViewMonitoring) |
| GET | `/api/monitoring/endpoints` | Get list of configured endpoints | Yes (ViewMonitoring) |
| GET | `/api/monitoring/history?hours={hours}` | Get monitoring history (max 168 hours) | Yes (ViewMetrics) |

### Configuration Management

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/configuration` | Get all configuration | Yes (Admin) |
| GET | `/api/configuration/monitoring` | Get monitoring configuration | Yes (Admin) |
| PUT | `/api/configuration/monitoring` | Update monitoring configuration | Yes (Admin) |
| GET | `/api/configuration/database-monitoring` | Get database monitoring config | Yes (Admin) |
| PUT | `/api/configuration/database-monitoring` | Update database monitoring config | Yes (Admin) |
| GET | `/api/configuration/quartz` | Get Quartz configuration | Yes (Admin) |
| GET | `/api/configuration/value/{key}` | Get configuration value by key | Yes (Admin) |
| PUT | `/api/configuration/value/{key}` | Set configuration value by key | Yes (Admin) |
| POST | `/api/configuration/reload` | Reload configuration from sources | Yes (Admin) |
| GET | `/api/configuration/history` | Get configuration change history | Yes (Admin) |

### Database Monitoring

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/database-monitoring/connections/test` | Test all database connections | Yes (ViewMonitoring) |
| GET | `/api/database-monitoring/connections/{name}/test` | Test specific connection | Yes (ViewMonitoring) |
| GET | `/api/database-monitoring/queries/execute` | Execute all monitoring queries | Yes (ViewMonitoring) |
| GET | `/api/database-monitoring/connections/{name}/queries` | Execute queries for connection | Yes (ViewMonitoring) |
| GET | `/api/database-monitoring/connections/{name}/queries/{query}` | Execute specific query | Yes (ViewMonitoring) |
| POST | `/api/database-monitoring/connections/{name}/custom-query` | Execute custom SQL query | Yes (Admin/Operator) |
| GET | `/api/database-monitoring/statistics` | Get database monitoring statistics | Yes (ViewMetrics) |
| GET | `/api/database-monitoring/connections` | Get list of configured connections | Yes (ViewMonitoring) |
| GET | `/api/database-monitoring/queries` | Get list of configured queries | Yes (ViewMonitoring) |

### SignalR Hub Methods

| Method | Description | Auth Required | Permission |
|--------|-------------|---------------|------------|
| `GetMetrics` | Request current metrics | Yes | ViewMetrics |
| `TriggerManualCheck` | Trigger manual monitoring check | Yes | ManageMonitoring |
| `GetUserInfo` | Get current user information | Yes | - |
| `Subscribe` | Subscribe to monitoring group | Yes | ViewMonitoring |
| `Unsubscribe` | Unsubscribe from monitoring group | Yes | ViewMonitoring |

### SignalR Hub Events

| Event | Description | Data Type |
|-------|-------------|-----------|
| `Connected` | User connected to hub | `{ ConnectionId, Username, UserId, Roles, Permissions }` |
| `MonitoringUpdate` | HTTP monitoring result | `MonitoringEvent` |
| `DatabaseMonitoringUpdate` | Database monitoring result | `DatabaseMonitoringResult` |
| `MetricsUpdate` | Current metrics data | `MetricsData` |
| `Error` | Error occurred | `{ Message, Timestamp, Error? }` |

## 🏥 Health Endpoints

- **Liveness**: `GET /healthz/live` - Returns 200 if the service is alive
- **Readiness**: `GET /healthz/ready` - Returns 200 if the service is ready to handle requests
- **Combined**: `GET /healthz` - Returns overall health status

### Health Check Response Format

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "duration": "00:00:00.0234567",
  "checks": [
    {
      "name": "liveness",
      "status": "Healthy",
      "duration": "00:00:00.0123456",
      "description": "Service is alive and responding",
      "data": {
        "uptime": "01.12:34:56",
        "startTime": "2024-01-01T00:00:00Z"
      }
    }
  ]
}
```

## 📊 Metrics

The service collects comprehensive metrics:

- **Worker Metrics**: Heartbeat counts
- **Job Metrics**: Started, completed, failed, cancelled counts
- **Check Metrics**: Per-endpoint status counts and response times
- **Duration Metrics**: Average response times and execution durations

Access metrics via SignalR:
```javascript
await connection.invoke("GetMetrics");
```

## 🔧 Adding New Monitoring Checks

1. **Add endpoint configuration** to `appsettings.json`:
   ```json
   {
     "Name": "DatabaseCheck",
     "Url": "https://api.example.com/db/health",
     "TimeoutSeconds": 15,
     "ExpectedStatusCodes": [200, 202]
   }
   ```

2. **For custom logic**, extend `MonitoringService.CheckEndpointAsync()` or create a new service implementing `IMonitoringService`

3. **Custom headers** can be added per endpoint:
   ```json
   {
     "Headers": {
       "Authorization": "Bearer token",
       "X-API-Key": "your-api-key"
     }
   }
   ```

## 🚀 Production Considerations

### Security

1. **JWT Secret Key**: Use a strong, randomly generated secret key (minimum 32 characters):
   ```bash
   # Generate a secure key
   openssl rand -base64 32
   ```

2. **Password Security**: Replace default passwords with strong, unique passwords or integrate with your identity provider:
   ```json
   {
     "Authentication": {
       "Users": {
         "DefaultUsers": [
           {
             "Username": "admin",
             "Password": "$2a$12$hashed_password_here", // Use BCrypt or similar
             "Roles": ["Admin"]
           }
         ]
       }
     }
   }
   ```

3. **Database Connection Security**: Use encrypted connections and least-privilege accounts:
   ```json
   {
     "ConnectionString": "Server=server;Database=db;User Id=monitor_user;Password=secure_password;Encrypt=true;TrustServerCertificate=false;"
   }
   ```

4. **HTTPS**: Always use HTTPS in production
5. **CORS**: Configure CORS appropriately for your environment
6. **Rate Limiting**: Implement rate limiting on API endpoints

### Scaling

1. **Redis Backplane**: For multiple instances:
   ```csharp
   services.AddSignalR()
       .AddStackExchangeRedis("redis-connection-string");
   ```

2. **Quartz Clustering**: Enable job clustering:
   ```csharp
   q.UsePersistentStore(x =>
   {
       x.UseProperties = true;
       x.UseSqlServer("connection-string");
       x.UseClustering();
   });
   ```

3. **Load Balancing**: Use nginx or cloud load balancers with sticky sessions for SignalR

### Monitoring & Observability

1. **OpenTelemetry**: Add distributed tracing:
   ```csharp
   services.AddOpenTelemetryTracing(builder =>
   {
       builder
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddQuartzInstrumentation()
           .AddJaegerExporter();
   });
   ```

2. **Prometheus Metrics**: Export metrics:
   ```csharp
   app.UseEndpoints(endpoints =>
   {
       endpoints.MapMetrics(); // /metrics endpoint
   });
   ```

3. **Structured Logging**: Serilog is configured for structured logging with enrichers

## 🐳 Docker Deployment

### Development
```bash
docker-compose up
```

### Production with Nginx
```bash
docker-compose --profile production up
```

### With Redis Scaling
```bash
docker-compose --profile scaling up
```

## 🧪 Testing Strategy

The solution includes **113 comprehensive unit tests** covering:

- **Authentication Service Tests**: JWT token generation, validation, user management, and role-based permissions
- **Database Monitoring Tests**: Connection testing, query execution, result evaluation, and error handling
- **Configuration Service Tests**: Validation, hot reload, change tracking, and environment-specific settings
- **Core Service Tests**: HTTP monitoring, metrics collection, event notifications, and job execution
- **Integration Tests**: End-to-end testing with TestServer and real dependencies
- **Mocking Strategy**: Extensive use of Moq for isolated testing of complex dependencies
- **Fluent Assertions**: Readable and maintainable test assertions with detailed error messages

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Authentication

# Run tests with detailed output
dotnet test --verbosity normal
```

### Test Coverage

- **Authentication**: 25 tests covering JWT, roles, permissions, and user management
- **Database Monitoring**: 28 tests covering connections, queries, validation, and error scenarios
- **Configuration**: 20 tests covering validation, updates, change tracking, and reload
- **Core Services**: 40 tests covering HTTP monitoring, metrics, notifications, and jobs

## 📝 Troubleshooting

### Common Issues

1. **Authentication Failed**:
   - Check JWT secret key length (minimum 32 characters)
   - Verify user credentials in configuration
   - Ensure token hasn't expired

2. **SignalR Connection Failed**:
   - Check CORS settings and firewall rules
   - Verify JWT token is included in connection
   - Check authentication configuration

3. **Database Connection Issues**:
   - Verify connection strings are correct
   - Check SQL Server is accessible and running
   - Ensure database user has required permissions
   - Check connection timeout settings

4. **Jobs Not Running**:
   - Verify Quartz configuration and cron expressions
   - Check logs for job execution errors
   - Ensure monitoring is enabled in configuration

5. **Permission Denied Errors**:
   - Check user roles and permissions
   - Verify JWT token contains correct claims
   - Ensure API endpoints have correct authorization policies

6. **Health Check Failing**:
   - Ensure dependent services are accessible
   - Check database connections if database monitoring is enabled
   - Verify authentication service is working

7. **High Memory Usage**:
   - Check for memory leaks in monitoring tasks
   - Monitor database connection pooling
   - Review concurrent connection limits

### Debug Logging

Enable debug logging in `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Quartz": "Debug",
        "Microsoft.AspNetCore.SignalR": "Debug"
      }
    }
  }
}
```

## 📄 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

---

## 🔄 CI/CD Pipeline

### GitHub Actions Example

```yaml
name: Build and Deploy

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Build Docker Image
      run: docker build -t monitoring-worker:${{ github.sha }} .

    - name: Push to Registry
      if: github.ref == 'refs/heads/main'
      run: |
        docker tag monitoring-worker:${{ github.sha }} myregistry/monitoring-worker:latest
        docker push myregistry/monitoring-worker:latest
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: monitoring-worker
spec:
  replicas: 2
  selector:
    matchLabels:
      app: monitoring-worker
  template:
    metadata:
      labels:
        app: monitoring-worker
    spec:
      containers:
      - name: monitoring-worker
        image: monitoring-worker:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: Monitoring__DefaultTimeoutSeconds
          value: "30"
        livenessProbe:
          httpGet:
            path: /healthz/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /healthz/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 10
```

**Built with ❤️ using .NET 8, Quartz.NET, SignalR, and modern development practices.**
