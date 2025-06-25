/*
================================================================================
MonitoringWorker Database Schema - Simplified Version
================================================================================
Database: PopAI (System Database)
Purpose: Create core tables for MonitoringWorker service
Date: 2025-06-25
Version: 1.0
================================================================================
*/

-- Enable error handling
SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT 'Creating MonitoringWorker tables...';

BEGIN TRY
    BEGIN TRANSACTION;

    -- ============================================================================
    -- 1. Worker Instances Table
    -- ============================================================================
    
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerInstances' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.WorkerInstances (
            WorkerInstanceId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            WorkerName NVARCHAR(100) NOT NULL,
            MachineName NVARCHAR(100) NOT NULL,
            ProcessId INT NOT NULL,
            Version NVARCHAR(50) NOT NULL,
            Environment NVARCHAR(50) NOT NULL DEFAULT 'Production',
            Status NVARCHAR(20) NOT NULL DEFAULT 'Starting',
            StartedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            StoppedAt DATETIME2 NULL,
            LastHeartbeat DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            Configuration NVARCHAR(MAX) NULL,
            Tags NVARCHAR(500) NULL,
            CreatedBy NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
            CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_WorkerInstances PRIMARY KEY (WorkerInstanceId),
            CONSTRAINT CK_WorkerInstances_Status CHECK (Status IN ('Starting', 'Running', 'Stopping', 'Stopped', 'Error'))
        );
        
        CREATE NONCLUSTERED INDEX IX_WorkerInstances_Status_LastHeartbeat 
            ON monitoring.WorkerInstances (Status, LastHeartbeat);
        
        PRINT '✓ Created monitoring.WorkerInstances table';
    END
    ELSE
        PRINT '⚠ monitoring.WorkerInstances table already exists';

    -- ============================================================================
    -- 2. Worker Jobs Table
    -- ============================================================================
    
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerJobs' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.WorkerJobs (
            JobId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            WorkerInstanceId UNIQUEIDENTIFIER NOT NULL,
            JobType NVARCHAR(50) NOT NULL,
            JobName NVARCHAR(200) NOT NULL,
            Status NVARCHAR(20) NOT NULL DEFAULT 'Queued',
            Priority INT NOT NULL DEFAULT 5,
            ScheduledAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            StartedAt DATETIME2 NULL,
            CompletedAt DATETIME2 NULL,
            DurationMs BIGINT NULL,
            ResultStatus NVARCHAR(20) NULL,
            ResultMessage NVARCHAR(1000) NULL,
            ResultData NVARCHAR(MAX) NULL,
            ErrorMessage NVARCHAR(MAX) NULL,
            RetryCount INT NOT NULL DEFAULT 0,
            MaxRetries INT NOT NULL DEFAULT 3,
            NextRetryAt DATETIME2 NULL,
            CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_WorkerJobs PRIMARY KEY (JobId),
            CONSTRAINT FK_WorkerJobs_WorkerInstances FOREIGN KEY (WorkerInstanceId) 
                REFERENCES monitoring.WorkerInstances(WorkerInstanceId) ON DELETE CASCADE,
            CONSTRAINT CK_WorkerJobs_Status CHECK (Status IN ('Queued', 'Running', 'Completed', 'Failed', 'Cancelled')),
            CONSTRAINT CK_WorkerJobs_Priority CHECK (Priority BETWEEN 1 AND 10)
        );
        
        CREATE NONCLUSTERED INDEX IX_WorkerJobs_Status_Priority 
            ON monitoring.WorkerJobs (Status, Priority, ScheduledAt);
        
        PRINT '✓ Created monitoring.WorkerJobs table';
    END
    ELSE
        PRINT '⚠ monitoring.WorkerJobs table already exists';

    -- ============================================================================
    -- 3. Database Connections Table
    -- ============================================================================
    
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseConnections' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.DatabaseConnections (
            ConnectionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            ConnectionName NVARCHAR(100) NOT NULL,
            Provider NVARCHAR(50) NOT NULL DEFAULT 'SqlServer',
            ConnectionString NVARCHAR(1000) NOT NULL,
            Environment NVARCHAR(50) NOT NULL,
            Tags NVARCHAR(500) NULL,
            ConnectionTimeoutSeconds INT NOT NULL DEFAULT 30,
            CommandTimeoutSeconds INT NOT NULL DEFAULT 30,
            IsEnabled BIT NOT NULL DEFAULT 1,
            IsEncrypted BIT NOT NULL DEFAULT 1,
            CreatedBy NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
            CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            ModifiedBy NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
            ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_DatabaseConnections PRIMARY KEY (ConnectionId),
            CONSTRAINT UQ_DatabaseConnections_Name UNIQUE (ConnectionName),
            CONSTRAINT CK_DatabaseConnections_Provider CHECK (Provider IN ('SqlServer', 'MySQL', 'PostgreSQL', 'Oracle'))
        );
        
        PRINT '✓ Created monitoring.DatabaseConnections table';
    END
    ELSE
        PRINT '⚠ monitoring.DatabaseConnections table already exists';

    -- ============================================================================
    -- 4. Database Queries Table
    -- ============================================================================
    
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseQueries' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.DatabaseQueries (
            QueryId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            QueryName NVARCHAR(100) NOT NULL,
            QueryType NVARCHAR(50) NOT NULL DEFAULT 'HealthCheck',
            SqlQuery NVARCHAR(MAX) NOT NULL,
            ResultType NVARCHAR(20) NOT NULL DEFAULT 'Scalar',
            ExpectedValue NVARCHAR(100) NULL,
            ComparisonOperator NVARCHAR(10) NULL DEFAULT 'eq',
            WarningThreshold DECIMAL(18,4) NULL,
            CriticalThreshold DECIMAL(18,4) NULL,
            TimeoutSeconds INT NOT NULL DEFAULT 30,
            IsEnabled BIT NOT NULL DEFAULT 1,
            Description NVARCHAR(500) NULL,
            CreatedBy NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
            CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            ModifiedBy NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
            ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_DatabaseQueries PRIMARY KEY (QueryId),
            CONSTRAINT UQ_DatabaseQueries_Name UNIQUE (QueryName),
            CONSTRAINT CK_DatabaseQueries_QueryType CHECK (QueryType IN ('HealthCheck', 'Performance', 'Custom')),
            CONSTRAINT CK_DatabaseQueries_ResultType CHECK (ResultType IN ('Scalar', 'NonQuery', 'DataSet'))
        );
        
        PRINT '✓ Created monitoring.DatabaseQueries table';
    END
    ELSE
        PRINT '⚠ monitoring.DatabaseQueries table already exists';

    -- ============================================================================
    -- 5. Database Monitoring Results Table
    -- ============================================================================
    
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseMonitoringResults' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.DatabaseMonitoringResults (
            ResultId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            JobId UNIQUEIDENTIFIER NOT NULL,
            ConnectionId UNIQUEIDENTIFIER NOT NULL,
            QueryId UNIQUEIDENTIFIER NULL,
            ConnectionName NVARCHAR(100) NOT NULL,
            QueryName NVARCHAR(100) NULL,
            Status NVARCHAR(20) NOT NULL,
            Message NVARCHAR(1000) NOT NULL,
            ResultValue NVARCHAR(MAX) NULL,
            DurationMs BIGINT NOT NULL,
            Provider NVARCHAR(50) NOT NULL,
            Environment NVARCHAR(50) NOT NULL,
            Tags NVARCHAR(500) NULL,
            ServerVersion NVARCHAR(100) NULL,
            DatabaseName NVARCHAR(100) NULL,
            Details NVARCHAR(MAX) NULL,
            Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_DatabaseMonitoringResults PRIMARY KEY (ResultId),
            CONSTRAINT FK_DatabaseMonitoringResults_Jobs FOREIGN KEY (JobId) 
                REFERENCES monitoring.WorkerJobs(JobId) ON DELETE CASCADE,
            CONSTRAINT FK_DatabaseMonitoringResults_Connections FOREIGN KEY (ConnectionId) 
                REFERENCES monitoring.DatabaseConnections(ConnectionId),
            CONSTRAINT FK_DatabaseMonitoringResults_Queries FOREIGN KEY (QueryId) 
                REFERENCES monitoring.DatabaseQueries(QueryId),
            CONSTRAINT CK_DatabaseMonitoringResults_Status CHECK (Status IN ('Healthy', 'Warning', 'Critical', 'Error'))
        );
        
        CREATE NONCLUSTERED INDEX IX_DatabaseMonitoringResults_Timestamp 
            ON monitoring.DatabaseMonitoringResults (Timestamp DESC);
        
        PRINT '✓ Created monitoring.DatabaseMonitoringResults table';
    END
    ELSE
        PRINT '⚠ monitoring.DatabaseMonitoringResults table already exists';

    -- ============================================================================
    -- 6. Worker Metrics Table
    -- ============================================================================
    
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerMetrics' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.WorkerMetrics (
            MetricId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            WorkerInstanceId UNIQUEIDENTIFIER NOT NULL,
            MetricType NVARCHAR(50) NOT NULL,
            MetricName NVARCHAR(100) NOT NULL,
            MetricValue DECIMAL(18,4) NOT NULL,
            Unit NVARCHAR(20) NULL,
            Tags NVARCHAR(500) NULL,
            Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_WorkerMetrics PRIMARY KEY (MetricId),
            CONSTRAINT FK_WorkerMetrics_WorkerInstances FOREIGN KEY (WorkerInstanceId) 
                REFERENCES monitoring.WorkerInstances(WorkerInstanceId) ON DELETE CASCADE
        );
        
        CREATE NONCLUSTERED INDEX IX_WorkerMetrics_Timestamp 
            ON monitoring.WorkerMetrics (Timestamp DESC);
        
        PRINT '✓ Created monitoring.WorkerMetrics table';
    END
    ELSE
        PRINT '⚠ monitoring.WorkerMetrics table already exists';

    -- ============================================================================
    -- 7. Insert Initial Data
    -- ============================================================================
    
    PRINT 'Inserting initial configuration data...';
    
    -- Insert default database queries
    IF NOT EXISTS (SELECT * FROM monitoring.DatabaseQueries WHERE QueryName = 'Connection Test')
    BEGIN
        INSERT INTO monitoring.DatabaseQueries (
            QueryName, QueryType, SqlQuery, ResultType, Description
        ) VALUES (
            'Connection Test', 'HealthCheck', 'SELECT 1 as Result', 'Scalar', 
            'Basic connection test query'
        );
        PRINT '✓ Inserted default Connection Test query';
    END

    -- Insert configuration values
    IF NOT EXISTS (SELECT * FROM monitoring.Config WHERE ConfigKey = 'MonitoringWorker.RetentionDays')
    BEGIN
        INSERT INTO monitoring.Config (ConfigKey, ConfigValue, Description, Category)
        VALUES ('MonitoringWorker.RetentionDays', '30', 'Number of days to retain monitoring data', 'MonitoringWorker');
        PRINT '✓ Inserted MonitoringWorker.RetentionDays configuration';
    END

    COMMIT TRANSACTION;
    
    PRINT '================================================================================';
    PRINT 'MonitoringWorker schema created successfully!';
    PRINT 'Tables created: WorkerInstances, WorkerJobs, DatabaseConnections,';
    PRINT '                DatabaseQueries, DatabaseMonitoringResults, WorkerMetrics';
    PRINT '================================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT '================================================================================';
    PRINT 'ERROR: Schema creation failed!';
    PRINT 'Error Message: ' + @ErrorMessage;
    PRINT '================================================================================';
    
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
