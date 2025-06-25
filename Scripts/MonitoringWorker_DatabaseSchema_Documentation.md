# MonitoringWorker Database Schema Documentation

## Overview

This document describes the database schema extension for the MonitoringWorker service in the PopAI system database. The schema builds upon the existing monitoring infrastructure and adds specialized tables for worker service management, database monitoring configuration, and performance tracking.

## Database Information

- **Database**: PopAI (System Database)
- **Server**: 192.168.166.11,1433
- **SQL Server Version**: Microsoft SQL Server 2022 Enterprise Edition
- **Schema Extension Version**: 1.0

## Existing Infrastructure Analysis

The PopAI database already contains a robust monitoring infrastructure:

### Existing Schemas
- **auth**: Complete authentication system with users, roles, permissions, and security features
- **monitoring**: Existing monitoring tables for indicators, execution history, alerts, contacts, configuration, and system status
- **dbo**: Entity Framework migrations and security audit events

### Key Existing Tables
- `monitoring.Indicators` - KPI and monitoring indicators
- `monitoring.ExecutionHistory` - Historical execution data
- `monitoring.AlertLogs` - Alert management
- `monitoring.Config` - System configuration
- `monitoring.SystemStatus` - Service status tracking
- `monitoring.Contacts` - Contact management for alerts

## New Schema Extensions

### 1. Worker Service Management Tables

#### `monitoring.WorkerInstances`
Tracks individual worker service instances and their lifecycle.

**Key Features:**
- Unique identification of worker instances
- Machine and process tracking
- Environment separation (Production, Development, etc.)
- Status lifecycle management
- Heartbeat monitoring
- JSON configuration storage

**Columns:**
- `WorkerInstanceId` (UNIQUEIDENTIFIER, PK) - Unique worker identifier
- `WorkerName` (NVARCHAR(100)) - Logical worker name
- `MachineName` (NVARCHAR(100)) - Host machine name
- `ProcessId` (INT) - Operating system process ID
- `Version` (NVARCHAR(50)) - Worker service version
- `Environment` (NVARCHAR(50)) - Environment (Production, Development, etc.)
- `Status` (NVARCHAR(20)) - Current status (Starting, Running, Stopping, Stopped, Error)
- `StartedAt` (DATETIME2) - Service start time
- `StoppedAt` (DATETIME2) - Service stop time (nullable)
- `LastHeartbeat` (DATETIME2) - Last heartbeat timestamp
- `Configuration` (NVARCHAR(MAX)) - JSON configuration data
- `Tags` (NVARCHAR(500)) - Comma-separated tags for categorization

#### `monitoring.WorkerJobs`
Tracks individual monitoring jobs executed by worker instances.

**Key Features:**
- Job queue management
- Priority-based execution
- Retry logic support
- Comprehensive result tracking
- Performance metrics

**Columns:**
- `JobId` (UNIQUEIDENTIFIER, PK) - Unique job identifier
- `WorkerInstanceId` (UNIQUEIDENTIFIER, FK) - Associated worker instance
- `JobType` (NVARCHAR(50)) - Type of job (DatabaseMonitoring, EndpointMonitoring, HealthCheck)
- `JobName` (NVARCHAR(200)) - Descriptive job name
- `Status` (NVARCHAR(20)) - Job status (Queued, Running, Completed, Failed, Cancelled)
- `Priority` (INT) - Job priority (1-10, lower is higher priority)
- `ScheduledAt` (DATETIME2) - When job was scheduled
- `StartedAt` (DATETIME2) - When job execution started
- `CompletedAt` (DATETIME2) - When job execution completed
- `DurationMs` (BIGINT) - Execution duration in milliseconds
- `ResultStatus` (NVARCHAR(20)) - Result status (Healthy, Warning, Critical, Error)
- `ResultMessage` (NVARCHAR(1000)) - Result message
- `ResultData` (NVARCHAR(MAX)) - JSON result data
- `ErrorMessage` (NVARCHAR(MAX)) - Error details if failed
- `RetryCount` (INT) - Current retry attempt
- `MaxRetries` (INT) - Maximum retry attempts
- `NextRetryAt` (DATETIME2) - Next retry timestamp

### 2. Database Monitoring Configuration Tables

#### `monitoring.DatabaseConnections`
Stores configuration for database connections to be monitored.

**Key Features:**
- Multi-provider support (SQL Server, MySQL, PostgreSQL, Oracle)
- Environment-based organization
- Encrypted connection strings
- Configurable timeouts
- Tag-based categorization

**Columns:**
- `ConnectionId` (UNIQUEIDENTIFIER, PK) - Unique connection identifier
- `ConnectionName` (NVARCHAR(100), UNIQUE) - Logical connection name
- `Provider` (NVARCHAR(50)) - Database provider type
- `ConnectionString` (NVARCHAR(1000)) - Encrypted connection string
- `Environment` (NVARCHAR(50)) - Environment classification
- `Tags` (NVARCHAR(500)) - Comma-separated tags
- `ConnectionTimeoutSeconds` (INT) - Connection timeout
- `CommandTimeoutSeconds` (INT) - Command execution timeout
- `IsEnabled` (BIT) - Whether connection is active
- `IsEncrypted` (BIT) - Whether connection string is encrypted

#### `monitoring.DatabaseQueries`
Defines monitoring queries to be executed against database connections.

**Key Features:**
- Multiple query types (HealthCheck, Performance, Custom)
- Flexible result handling (Scalar, NonQuery, DataSet)
- Threshold-based alerting
- Comparison operators for result evaluation

**Columns:**
- `QueryId` (UNIQUEIDENTIFIER, PK) - Unique query identifier
- `QueryName` (NVARCHAR(100), UNIQUE) - Logical query name
- `QueryType` (NVARCHAR(50)) - Query classification
- `SqlQuery` (NVARCHAR(MAX)) - SQL query text
- `ResultType` (NVARCHAR(20)) - Expected result type
- `ExpectedValue` (NVARCHAR(100)) - Expected result value
- `ComparisonOperator` (NVARCHAR(10)) - Comparison logic (eq, ne, gt, lt, gte, lte, contains)
- `WarningThreshold` (DECIMAL(18,4)) - Warning threshold value
- `CriticalThreshold` (DECIMAL(18,4)) - Critical threshold value
- `TimeoutSeconds` (INT) - Query execution timeout

### 3. Monitoring Results and Metrics Tables

#### `monitoring.DatabaseMonitoringResults`
Stores results from database monitoring operations.

**Key Features:**
- Comprehensive result tracking
- Performance metrics
- Environment and provider tracking
- JSON details storage
- Time-series data for trending

#### `monitoring.WorkerMetrics`
Stores performance metrics from worker instances.

**Key Features:**
- Multiple metric types (Heartbeat, JobExecution, Performance, Error)
- Flexible metric value storage
- Unit tracking
- Tag-based categorization
- Time-series data for analysis

### 4. Monitoring Views

#### `monitoring.vw_WorkerStatusOverview`
Provides real-time overview of worker instance health and status.

**Features:**
- Health status calculation based on heartbeat
- Running and queued job counts
- Time since last heartbeat
- Environment-based filtering

#### `monitoring.vw_DatabaseMonitoringSummary`
Summarizes database monitoring results and health statistics.

**Features:**
- Last check status and timing
- 7-day health statistics
- Success rate calculations
- Environment and provider grouping

### 5. Stored Procedures

#### `monitoring.sp_RegisterWorkerInstance`
Registers a new worker instance in the system.

**Parameters:**
- `@WorkerName` - Logical worker name
- `@MachineName` - Host machine name
- `@ProcessId` - Process ID
- `@Version` - Worker version
- `@Environment` - Environment name
- `@Configuration` - JSON configuration (optional)
- `@Tags` - Tags (optional)
- `@WorkerInstanceId` (OUTPUT) - Generated worker ID

#### `monitoring.sp_UpdateWorkerHeartbeat`
Updates worker heartbeat and status.

**Parameters:**
- `@WorkerInstanceId` - Worker instance ID
- `@Status` - Current status (default: 'Running')

#### `monitoring.sp_CleanupMonitoringData`
Performs maintenance cleanup of old monitoring data.

**Parameters:**
- `@RetentionDays` - Days to retain data (default: 30)
- `@BatchSize` - Cleanup batch size (default: 1000)

## Integration with Existing Infrastructure

### Configuration Management
The new schema integrates with the existing `monitoring.Config` table by adding MonitoringWorker-specific configuration keys:

- `MonitoringWorker.RetentionDays` - Data retention period
- `MonitoringWorker.MaxConcurrentJobs` - Concurrent job limit

### System Status Integration
The existing `monitoring.SystemStatus` table is extended with a `WorkerInstanceId` column to link system status records with specific worker instances.

### Alert Integration
The new monitoring results can integrate with the existing `monitoring.AlertLogs` table for alert generation based on monitoring thresholds.

## Security Considerations

1. **Connection String Encryption**: Database connection strings are stored encrypted
2. **Access Control**: All tables respect the existing authentication and authorization framework
3. **Audit Trail**: All operations include created/modified tracking
4. **Data Retention**: Automatic cleanup procedures prevent data accumulation

## Performance Optimizations

1. **Indexing Strategy**: Comprehensive indexes for time-series queries and status lookups
2. **Partitioning Ready**: Large tables designed for future partitioning by date
3. **Batch Operations**: Cleanup procedures use batching to minimize blocking
4. **Query Optimization**: Views designed for efficient dashboard queries

## Deployment Instructions

1. **Prerequisites**: Ensure user has `db_ddladmin`, `db_datawriter`, and `db_datareader` permissions
2. **Execution**: Run the `MonitoringWorker_DatabaseSchema.sql` script
3. **Verification**: Check script output for successful table creation
4. **Testing**: Verify views and procedures work correctly
5. **Configuration**: Update application connection strings and configuration

## Maintenance Procedures

1. **Regular Cleanup**: Schedule `sp_CleanupMonitoringData` to run weekly
2. **Index Maintenance**: Include new tables in existing index maintenance plans
3. **Backup Strategy**: Ensure new tables are included in backup procedures
4. **Monitoring**: Monitor table growth and performance metrics

## Next Steps

1. **Application Integration**: Update MonitoringWorker service to use new schema
2. **Dashboard Development**: Create monitoring dashboards using the new views
3. **Alert Configuration**: Set up alerts based on monitoring thresholds
4. **Performance Tuning**: Monitor and optimize based on actual usage patterns
