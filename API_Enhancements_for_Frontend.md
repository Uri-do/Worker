# API Enhancements for Frontend Integration

## Overview

Based on the new database schema implementation and frontend requirements, several API enhancements are needed to provide comprehensive database monitoring capabilities to the frontend. This document outlines the required additions and modifications.

## Current API Status

### Existing Controllers
1. **WorkerController** - Worker lifecycle management ✅
2. **MonitoringController** - Endpoint monitoring ✅  
3. **DatabaseMonitoringController** - Basic database monitoring ✅
4. **AuthController** - Authentication ✅

### Existing Endpoints Working Well
- `GET /api/worker/status` - Worker status and metrics
- `POST /api/worker/start|stop|restart` - Worker lifecycle
- `GET /api/monitoring/status` - Endpoint monitoring status
- `POST /api/monitoring/check` - Manual endpoint checks
- `GET /api/database-monitoring/connections/test` - Connection testing

## Required API Enhancements

### 1. Enhanced Database Monitoring Endpoints

#### A. Worker Instance Management
```http
# Get all worker instances
GET /api/worker/instances
Response: WorkerInstance[]

# Get specific worker instance
GET /api/worker/instances/{instanceId}
Response: WorkerInstance

# Register new worker instance (internal use)
POST /api/worker/instances
Body: RegisterWorkerRequest
Response: WorkerInstance

# Update worker heartbeat (internal use)
PUT /api/worker/instances/{instanceId}/heartbeat
Response: OperationResult
```

#### B. Database Connection Management
```http
# Get all database connections
GET /api/database-monitoring/connections
Response: DatabaseConnection[]

# Get specific connection
GET /api/database-monitoring/connections/{connectionId}
Response: DatabaseConnection

# Create new database connection
POST /api/database-monitoring/connections
Body: CreateDatabaseConnectionRequest
Response: DatabaseConnection

# Update database connection
PUT /api/database-monitoring/connections/{connectionId}
Body: UpdateDatabaseConnectionRequest
Response: DatabaseConnection

# Delete database connection
DELETE /api/database-monitoring/connections/{connectionId}
Response: OperationResult

# Test specific connection
POST /api/database-monitoring/connections/{connectionId}/test
Response: DatabaseConnectionHealth
```

#### C. Database Query Management
```http
# Get all monitoring queries
GET /api/database-monitoring/queries
Response: DatabaseQuery[]

# Get specific query
GET /api/database-monitoring/queries/{queryId}
Response: DatabaseQuery

# Create new monitoring query
POST /api/database-monitoring/queries
Body: CreateDatabaseQueryRequest
Response: DatabaseQuery

# Update monitoring query
PUT /api/database-monitoring/queries/{queryId}
Body: UpdateDatabaseQueryRequest
Response: DatabaseQuery

# Delete monitoring query
DELETE /api/database-monitoring/queries/{queryId}
Response: OperationResult

# Test specific query
POST /api/database-monitoring/queries/{queryId}/test
Body: TestQueryRequest
Response: DatabaseMonitoringResult
```

#### D. Monitoring Results and History
```http
# Get monitoring results with filtering
GET /api/database-monitoring/results
Query: ?connectionId=&queryId=&status=&environment=&fromDate=&toDate=&pageSize=&pageNumber=
Response: PagedResult<DatabaseMonitoringResult>

# Get monitoring results for specific connection
GET /api/database-monitoring/connections/{connectionId}/results
Query: ?fromDate=&toDate=&pageSize=&pageNumber=
Response: PagedResult<DatabaseMonitoringResult>

# Get monitoring dashboard summary
GET /api/database-monitoring/dashboard
Response: DatabaseMonitoringDashboard

# Get monitoring trends
GET /api/database-monitoring/trends
Query: ?period=24h|7d|30d&groupBy=hour|day
Response: MonitoringTrends
```

#### E. Worker Jobs Management
```http
# Get all worker jobs
GET /api/worker/jobs
Query: ?status=&jobType=&workerId=&pageSize=&pageNumber=
Response: PagedResult<WorkerJob>

# Get specific job
GET /api/worker/jobs/{jobId}
Response: WorkerJob

# Cancel job
POST /api/worker/jobs/{jobId}/cancel
Response: OperationResult

# Retry failed job
POST /api/worker/jobs/{jobId}/retry
Response: OperationResult

# Get job execution history
GET /api/worker/jobs/{jobId}/history
Response: JobExecutionHistory[]
```

#### F. Metrics and Analytics
```http
# Get worker metrics
GET /api/worker/metrics
Query: ?workerId=&metricType=&fromDate=&toDate=
Response: WorkerMetric[]

# Get database monitoring analytics
GET /api/database-monitoring/analytics
Query: ?period=24h|7d|30d
Response: DatabaseMonitoringAnalytics

# Get performance metrics
GET /api/database-monitoring/performance
Query: ?connectionId=&period=24h|7d|30d
Response: PerformanceMetrics
```

### 2. New Response Models

#### WorkerInstance Model
```csharp
public class WorkerInstance
{
    public Guid WorkerInstanceId { get; set; }
    public string WorkerName { get; set; }
    public string MachineName { get; set; }
    public int ProcessId { get; set; }
    public string Version { get; set; }
    public string Environment { get; set; }
    public string Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public object? Configuration { get; set; }
    public List<string> Tags { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

#### DatabaseConnection Model
```csharp
public class DatabaseConnection
{
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; }
    public string Provider { get; set; }
    public string Environment { get; set; }
    public List<string> Tags { get; set; }
    public int ConnectionTimeoutSeconds { get; set; }
    public int CommandTimeoutSeconds { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    // ConnectionString excluded for security
}
```

#### DatabaseQuery Model
```csharp
public class DatabaseQuery
{
    public Guid QueryId { get; set; }
    public string QueryName { get; set; }
    public string QueryType { get; set; }
    public string SqlQuery { get; set; }
    public string ResultType { get; set; }
    public string? ExpectedValue { get; set; }
    public string? ComparisonOperator { get; set; }
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

#### WorkerJob Model
```csharp
public class WorkerJob
{
    public Guid JobId { get; set; }
    public Guid WorkerInstanceId { get; set; }
    public string JobType { get; set; }
    public string JobName { get; set; }
    public string Status { get; set; }
    public int Priority { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ResultStatus { get; set; }
    public string? ResultMessage { get; set; }
    public object? ResultData { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

#### DatabaseMonitoringDashboard Model
```csharp
public class DatabaseMonitoringDashboard
{
    public DashboardSummary Summary { get; set; }
    public List<ConnectionStatus> Connections { get; set; }
    public List<RecentResult> RecentResults { get; set; }
    public List<WorkerStatus> Workers { get; set; }
    public PerformanceOverview Performance { get; set; }
}

public class DashboardSummary
{
    public int TotalConnections { get; set; }
    public int HealthyConnections { get; set; }
    public int WarningConnections { get; set; }
    public int CriticalConnections { get; set; }
    public int ErrorConnections { get; set; }
    public double OverallHealthPercentage { get; set; }
    public int ActiveWorkers { get; set; }
    public int RunningJobs { get; set; }
    public int QueuedJobs { get; set; }
    public DateTime LastUpdate { get; set; }
}
```

### 3. Request Models

#### CreateDatabaseConnectionRequest
```csharp
public class CreateDatabaseConnectionRequest
{
    [Required]
    public string ConnectionName { get; set; }
    
    [Required]
    public string Provider { get; set; }
    
    [Required]
    public string ConnectionString { get; set; }
    
    [Required]
    public string Environment { get; set; }
    
    public List<string>? Tags { get; set; }
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool IsEnabled { get; set; } = true;
}
```

#### CreateDatabaseQueryRequest
```csharp
public class CreateDatabaseQueryRequest
{
    [Required]
    public string QueryName { get; set; }
    
    [Required]
    public string QueryType { get; set; }
    
    [Required]
    public string SqlQuery { get; set; }
    
    public string ResultType { get; set; } = "Scalar";
    public string? ExpectedValue { get; set; }
    public string? ComparisonOperator { get; set; }
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}
```

### 4. Enhanced SignalR Events

#### Additional Hub Events
```csharp
// Database monitoring events
public async Task NotifyDatabaseMonitoringResult(DatabaseMonitoringResult result)
public async Task NotifyConnectionStatusChanged(string connectionName, string status)
public async Task NotifyWorkerInstanceStatusChanged(Guid workerId, string status)
public async Task NotifyJobStatusChanged(Guid jobId, string status)

// Dashboard updates
public async Task NotifyDashboardUpdate(DatabaseMonitoringDashboard dashboard)
public async Task NotifyMetricsUpdate(WorkerMetrics metrics)
```

### 5. Filtering and Pagination Support

#### Query Parameters
```csharp
public class DatabaseMonitoringResultsQuery : PagedQuery
{
    public Guid? ConnectionId { get; set; }
    public Guid? QueryId { get; set; }
    public string? Status { get; set; }
    public string? Environment { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Provider { get; set; }
}

public class PagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
```

## Implementation Priority

### Phase 1 (High Priority)
1. **Database Connection Management** - CRUD operations for connections
2. **Database Query Management** - CRUD operations for queries  
3. **Enhanced Monitoring Results** - Filtering and pagination
4. **Dashboard Summary** - Real-time overview

### Phase 2 (Medium Priority)
1. **Worker Instance Management** - Instance tracking and status
2. **Job Management** - Job queue and execution tracking
3. **Analytics and Trends** - Historical data analysis
4. **Performance Metrics** - Detailed performance tracking

### Phase 3 (Lower Priority)
1. **Advanced Filtering** - Complex query capabilities
2. **Bulk Operations** - Mass configuration changes
3. **Export Capabilities** - Data export functionality
4. **Advanced Analytics** - Predictive analysis

## Security Considerations

### Authentication & Authorization
- All endpoints require JWT authentication
- Role-based access control for different operations
- Sensitive data (connection strings) properly encrypted
- Audit logging for all configuration changes

### Data Protection
- Connection strings never returned in API responses
- Sensitive query parameters masked in logs
- Rate limiting on expensive operations
- Input validation and sanitization

## Frontend Integration Benefits

### Real-time Monitoring
- Live dashboard updates via SignalR
- Instant notification of status changes
- Real-time job execution tracking

### Comprehensive Management
- Full CRUD operations for all entities
- Bulk operations for efficiency
- Advanced filtering and search

### Analytics and Insights
- Historical trend analysis
- Performance metrics visualization
- Predictive health indicators

### User Experience
- Responsive pagination for large datasets
- Efficient caching strategies
- Optimized API responses

This enhancement plan provides a comprehensive API foundation for building a robust frontend monitoring interface that can effectively manage and monitor database connections, queries, and worker instances.
