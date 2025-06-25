/*
================================================================================
MonitoringWorker Database Schema Extension Script
================================================================================
Database: PopAI (System Database)
Purpose: Extend existing monitoring infrastructure to support MonitoringWorker
Author: MonitoringWorker Implementation
Date: 2025-06-25
Version: 1.0

IMPORTANT: This script extends the existing PopAI database schema.
The database already contains a robust monitoring infrastructure.
This script adds tables specifically for the MonitoringWorker service.

Prerequisites:
- SQL Server 2019 or later
- PopAI database exists with existing monitoring schema
- User has appropriate permissions (db_ddladmin, db_datawriter, db_datareader)

================================================================================
*/

-- Enable error handling and transaction management
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Declare variables for script execution
DECLARE @ErrorMessage NVARCHAR(4000);
DECLARE @ErrorSeverity INT;
DECLARE @ErrorState INT;
DECLARE @ScriptVersion NVARCHAR(10) = '1.0';
DECLARE @ExecutionStart DATETIME2 = SYSUTCDATETIME();

PRINT '================================================================================';
PRINT 'MonitoringWorker Database Schema Extension Script v' + @ScriptVersion;
PRINT 'Execution started at: ' + CONVERT(NVARCHAR(30), @ExecutionStart, 121);
PRINT '================================================================================';

BEGIN TRY
    BEGIN TRANSACTION MonitoringWorkerSchemaUpdate;

    -- ============================================================================
    -- 1. WORKER SERVICE MANAGEMENT TABLES
    -- ============================================================================
    
    PRINT 'Creating Worker Service Management tables...';
    
    -- Worker instances and their lifecycle management
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerInstances' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.WorkerInstances (
            WorkerInstanceId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            WorkerName NVARCHAR(100) NOT NULL,
            MachineName NVARCHAR(100) NOT NULL,
            ProcessId INT NOT NULL,
            Version NVARCHAR(50) NOT NULL,
            Environment NVARCHAR(50) NOT NULL DEFAULT 'Production',
            Status NVARCHAR(20) NOT NULL DEFAULT 'Starting', -- Starting, Running, Stopping, Stopped, Error
            StartedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            StoppedAt DATETIME2 NULL,
            LastHeartbeat DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            Configuration NVARCHAR(MAX) NULL, -- JSON configuration
            Tags NVARCHAR(500) NULL, -- Comma-separated tags
            CreatedBy NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
            CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            
            CONSTRAINT PK_WorkerInstances PRIMARY KEY (WorkerInstanceId),
            CONSTRAINT CK_WorkerInstances_Status CHECK (Status IN ('Starting', 'Running', 'Stopping', 'Stopped', 'Error')),
            CONSTRAINT CK_WorkerInstances_Dates CHECK (StoppedAt IS NULL OR StoppedAt >= StartedAt)
        );
        
        CREATE NONCLUSTERED INDEX IX_WorkerInstances_Status_LastHeartbeat 
            ON monitoring.WorkerInstances (Status, LastHeartbeat);
        CREATE NONCLUSTERED INDEX IX_WorkerInstances_MachineName_Status 
            ON monitoring.WorkerInstances (MachineName, Status);
        
        PRINT '  ✓ Created monitoring.WorkerInstances table';
    END
    ELSE
        PRINT '  ⚠ monitoring.WorkerInstances table already exists';

    -- Worker job execution tracking
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerJobs' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.WorkerJobs (
            JobId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            WorkerInstanceId UNIQUEIDENTIFIER NOT NULL,
            JobType NVARCHAR(50) NOT NULL, -- DatabaseMonitoring, EndpointMonitoring, HealthCheck
            JobName NVARCHAR(200) NOT NULL,
            Status NVARCHAR(20) NOT NULL DEFAULT 'Queued', -- Queued, Running, Completed, Failed, Cancelled
            Priority INT NOT NULL DEFAULT 5, -- 1-10, lower is higher priority
            ScheduledAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            StartedAt DATETIME2 NULL,
            CompletedAt DATETIME2 NULL,
            DurationMs BIGINT NULL,
            ResultStatus NVARCHAR(20) NULL, -- Healthy, Warning, Critical, Error
            ResultMessage NVARCHAR(1000) NULL,
            ResultData NVARCHAR(MAX) NULL, -- JSON result data
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
            CONSTRAINT CK_WorkerJobs_ResultStatus CHECK (ResultStatus IS NULL OR ResultStatus IN ('Healthy', 'Warning', 'Critical', 'Error')),
            CONSTRAINT CK_WorkerJobs_Priority CHECK (Priority BETWEEN 1 AND 10),
            CONSTRAINT CK_WorkerJobs_Dates CHECK (
                (StartedAt IS NULL OR StartedAt >= ScheduledAt) AND
                (CompletedAt IS NULL OR (StartedAt IS NOT NULL AND CompletedAt >= StartedAt))
            )
        );
        
        CREATE NONCLUSTERED INDEX IX_WorkerJobs_Status_Priority_ScheduledAt 
            ON monitoring.WorkerJobs (Status, Priority, ScheduledAt);
        CREATE NONCLUSTERED INDEX IX_WorkerJobs_WorkerInstance_Status 
            ON monitoring.WorkerJobs (WorkerInstanceId, Status);
        CREATE NONCLUSTERED INDEX IX_WorkerJobs_JobType_Status 
            ON monitoring.WorkerJobs (JobType, Status);
        CREATE NONCLUSTERED INDEX IX_WorkerJobs_NextRetryAt 
            ON monitoring.WorkerJobs (NextRetryAt) WHERE NextRetryAt IS NOT NULL;
        
        PRINT '  ✓ Created monitoring.WorkerJobs table';
    END
    ELSE
        PRINT '  ⚠ monitoring.WorkerJobs table already exists';

    -- ============================================================================
    -- 2. DATABASE MONITORING CONFIGURATION TABLES
    -- ============================================================================
    
    PRINT 'Creating Database Monitoring Configuration tables...';
    
    -- Database connections configuration
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseConnections' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.DatabaseConnections (
            ConnectionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            ConnectionName NVARCHAR(100) NOT NULL,
            Provider NVARCHAR(50) NOT NULL DEFAULT 'SqlServer', -- SqlServer, MySQL, PostgreSQL, Oracle
            ConnectionString NVARCHAR(1000) NOT NULL, -- Encrypted
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
            CONSTRAINT CK_DatabaseConnections_Provider CHECK (Provider IN ('SqlServer', 'MySQL', 'PostgreSQL', 'Oracle')),
            CONSTRAINT CK_DatabaseConnections_Timeouts CHECK (
                ConnectionTimeoutSeconds BETWEEN 1 AND 300 AND 
                CommandTimeoutSeconds BETWEEN 1 AND 300
            )
        );
        
        CREATE NONCLUSTERED INDEX IX_DatabaseConnections_Environment_Enabled 
            ON monitoring.DatabaseConnections (Environment, IsEnabled);
        
        PRINT '  ✓ Created monitoring.DatabaseConnections table';
    END
    ELSE
        PRINT '  ⚠ monitoring.DatabaseConnections table already exists';

    -- Database monitoring queries configuration
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseQueries' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.DatabaseQueries (
            QueryId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            QueryName NVARCHAR(100) NOT NULL,
            QueryType NVARCHAR(50) NOT NULL DEFAULT 'HealthCheck', -- HealthCheck, Performance, Custom
            SqlQuery NVARCHAR(MAX) NOT NULL,
            ResultType NVARCHAR(20) NOT NULL DEFAULT 'Scalar', -- Scalar, NonQuery, DataSet
            ExpectedValue NVARCHAR(100) NULL,
            ComparisonOperator NVARCHAR(10) NULL DEFAULT 'eq', -- eq, ne, gt, lt, gte, lte, contains
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
            CONSTRAINT CK_DatabaseQueries_ResultType CHECK (ResultType IN ('Scalar', 'NonQuery', 'DataSet')),
            CONSTRAINT CK_DatabaseQueries_ComparisonOperator CHECK (
                ComparisonOperator IS NULL OR 
                ComparisonOperator IN ('eq', 'ne', 'gt', 'lt', 'gte', 'lte', 'contains')
            ),
            CONSTRAINT CK_DatabaseQueries_Timeout CHECK (TimeoutSeconds BETWEEN 1 AND 300)
        );
        
        CREATE NONCLUSTERED INDEX IX_DatabaseQueries_QueryType_Enabled 
            ON monitoring.DatabaseQueries (QueryType, IsEnabled);
        
        PRINT '  ✓ Created monitoring.DatabaseQueries table';
    END
    ELSE
        PRINT '  ⚠ monitoring.DatabaseQueries table already exists';

    COMMIT TRANSACTION MonitoringWorkerSchemaUpdate;
    
    PRINT '================================================================================';
    PRINT 'Schema extension completed successfully!';
    PRINT 'Execution time: ' + CAST(DATEDIFF(MILLISECOND, @ExecutionStart, SYSUTCDATETIME()) AS NVARCHAR(10)) + ' ms';
    PRINT '================================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION MonitoringWorkerSchemaUpdate;
    
    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();
    
    PRINT '================================================================================';
    PRINT 'ERROR: Schema extension failed!';
    PRINT 'Error Message: ' + @ErrorMessage;
    PRINT 'Error Severity: ' + CAST(@ErrorSeverity AS NVARCHAR(10));
    PRINT 'Error State: ' + CAST(@ErrorState AS NVARCHAR(10));
    PRINT '================================================================================';
    
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

    -- ============================================================================
    -- 3. MONITORING RESULTS AND METRICS TABLES
    -- ============================================================================

    PRINT 'Creating Monitoring Results and Metrics tables...';

    -- Database monitoring results (extends existing monitoring capabilities)
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseMonitoringResults' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.DatabaseMonitoringResults (
            ResultId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            JobId UNIQUEIDENTIFIER NOT NULL,
            ConnectionId UNIQUEIDENTIFIER NOT NULL,
            QueryId UNIQUEIDENTIFIER NULL,
            ConnectionName NVARCHAR(100) NOT NULL,
            QueryName NVARCHAR(100) NULL,
            Status NVARCHAR(20) NOT NULL, -- Healthy, Warning, Critical, Error
            Message NVARCHAR(1000) NOT NULL,
            ResultValue NVARCHAR(MAX) NULL,
            DurationMs BIGINT NOT NULL,
            Provider NVARCHAR(50) NOT NULL,
            Environment NVARCHAR(50) NOT NULL,
            Tags NVARCHAR(500) NULL,
            ServerVersion NVARCHAR(100) NULL,
            DatabaseName NVARCHAR(100) NULL,
            Details NVARCHAR(MAX) NULL, -- JSON details
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

        CREATE NONCLUSTERED INDEX IX_DatabaseMonitoringResults_Timestamp_Status
            ON monitoring.DatabaseMonitoringResults (Timestamp DESC, Status);
        CREATE NONCLUSTERED INDEX IX_DatabaseMonitoringResults_Connection_Timestamp
            ON monitoring.DatabaseMonitoringResults (ConnectionId, Timestamp DESC);
        CREATE NONCLUSTERED INDEX IX_DatabaseMonitoringResults_Environment_Status
            ON monitoring.DatabaseMonitoringResults (Environment, Status, Timestamp DESC);

        PRINT '  ✓ Created monitoring.DatabaseMonitoringResults table';
    END
    ELSE
        PRINT '  ⚠ monitoring.DatabaseMonitoringResults table already exists';

    -- Worker performance metrics
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkerMetrics' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        CREATE TABLE monitoring.WorkerMetrics (
            MetricId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
            WorkerInstanceId UNIQUEIDENTIFIER NOT NULL,
            MetricType NVARCHAR(50) NOT NULL, -- Heartbeat, JobExecution, Performance, Error
            MetricName NVARCHAR(100) NOT NULL,
            MetricValue DECIMAL(18,4) NOT NULL,
            Unit NVARCHAR(20) NULL, -- ms, count, percent, bytes, etc.
            Tags NVARCHAR(500) NULL,
            Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

            CONSTRAINT PK_WorkerMetrics PRIMARY KEY (MetricId),
            CONSTRAINT FK_WorkerMetrics_WorkerInstances FOREIGN KEY (WorkerInstanceId)
                REFERENCES monitoring.WorkerInstances(WorkerInstanceId) ON DELETE CASCADE
        );

        CREATE NONCLUSTERED INDEX IX_WorkerMetrics_WorkerInstance_Timestamp
            ON monitoring.WorkerMetrics (WorkerInstanceId, Timestamp DESC);
        CREATE NONCLUSTERED INDEX IX_WorkerMetrics_MetricType_Timestamp
            ON monitoring.WorkerMetrics (MetricType, Timestamp DESC);
        CREATE NONCLUSTERED INDEX IX_WorkerMetrics_MetricName_Timestamp
            ON monitoring.WorkerMetrics (MetricName, Timestamp DESC);

        PRINT '  ✓ Created monitoring.WorkerMetrics table';
    END
    ELSE
        PRINT '  ⚠ monitoring.WorkerMetrics table already exists';

    -- ============================================================================
    -- 4. EXTEND EXISTING TABLES (ADD COLUMNS IF NEEDED)
    -- ============================================================================

    PRINT 'Checking for required extensions to existing tables...';

    -- Add WorkerInstanceId to existing SystemStatus table if not exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.SystemStatus') AND name = 'WorkerInstanceId')
    BEGIN
        ALTER TABLE monitoring.SystemStatus
        ADD WorkerInstanceId UNIQUEIDENTIFIER NULL,
            CONSTRAINT FK_SystemStatus_WorkerInstances FOREIGN KEY (WorkerInstanceId)
                REFERENCES monitoring.WorkerInstances(WorkerInstanceId);

        PRINT '  ✓ Added WorkerInstanceId column to monitoring.SystemStatus';
    END
    ELSE
        PRINT '  ⚠ WorkerInstanceId column already exists in monitoring.SystemStatus';

    -- ============================================================================
    -- 5. CREATE VIEWS FOR MONITORING DASHBOARD
    -- ============================================================================

    PRINT 'Creating monitoring views...';

    -- Worker status overview
    IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_WorkerStatusOverview' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        EXEC('
        CREATE VIEW monitoring.vw_WorkerStatusOverview AS
        SELECT
            wi.WorkerInstanceId,
            wi.WorkerName,
            wi.MachineName,
            wi.Environment,
            wi.Status,
            wi.StartedAt,
            wi.LastHeartbeat,
            DATEDIFF(SECOND, wi.LastHeartbeat, SYSUTCDATETIME()) AS SecondsSinceLastHeartbeat,
            CASE
                WHEN wi.Status = ''Running'' AND DATEDIFF(SECOND, wi.LastHeartbeat, SYSUTCDATETIME()) <= 300 THEN ''Healthy''
                WHEN wi.Status = ''Running'' AND DATEDIFF(SECOND, wi.LastHeartbeat, SYSUTCDATETIME()) <= 600 THEN ''Warning''
                ELSE ''Critical''
            END AS HealthStatus,
            (SELECT COUNT(*) FROM monitoring.WorkerJobs wj WHERE wj.WorkerInstanceId = wi.WorkerInstanceId AND wj.Status = ''Running'') AS RunningJobs,
            (SELECT COUNT(*) FROM monitoring.WorkerJobs wj WHERE wj.WorkerInstanceId = wi.WorkerInstanceId AND wj.Status = ''Queued'') AS QueuedJobs
        FROM monitoring.WorkerInstances wi
        WHERE wi.Status != ''Stopped''
        ');

        PRINT '  ✓ Created monitoring.vw_WorkerStatusOverview view';
    END
    ELSE
        PRINT '  ⚠ monitoring.vw_WorkerStatusOverview view already exists';

    -- Database monitoring summary
    IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_DatabaseMonitoringSummary' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        EXEC('
        CREATE VIEW monitoring.vw_DatabaseMonitoringSummary AS
        SELECT
            dc.ConnectionName,
            dc.Environment,
            dc.Provider,
            dc.IsEnabled,
            ISNULL(latest.Status, ''Unknown'') AS LastStatus,
            latest.Timestamp AS LastCheckTime,
            DATEDIFF(MINUTE, latest.Timestamp, SYSUTCDATETIME()) AS MinutesSinceLastCheck,
            stats.TotalChecks,
            stats.HealthyChecks,
            stats.WarningChecks,
            stats.CriticalChecks,
            stats.ErrorChecks,
            CASE
                WHEN stats.TotalChecks > 0
                THEN CAST(stats.HealthyChecks AS DECIMAL(10,2)) / stats.TotalChecks * 100
                ELSE 0
            END AS HealthyPercentage
        FROM monitoring.DatabaseConnections dc
        LEFT JOIN (
            SELECT
                ConnectionId,
                Status,
                Timestamp,
                ROW_NUMBER() OVER (PARTITION BY ConnectionId ORDER BY Timestamp DESC) as rn
            FROM monitoring.DatabaseMonitoringResults
        ) latest ON dc.ConnectionId = latest.ConnectionId AND latest.rn = 1
        LEFT JOIN (
            SELECT
                ConnectionId,
                COUNT(*) as TotalChecks,
                SUM(CASE WHEN Status = ''Healthy'' THEN 1 ELSE 0 END) as HealthyChecks,
                SUM(CASE WHEN Status = ''Warning'' THEN 1 ELSE 0 END) as WarningChecks,
                SUM(CASE WHEN Status = ''Critical'' THEN 1 ELSE 0 END) as CriticalChecks,
                SUM(CASE WHEN Status = ''Error'' THEN 1 ELSE 0 END) as ErrorChecks
            FROM monitoring.DatabaseMonitoringResults
            WHERE Timestamp >= DATEADD(DAY, -7, SYSUTCDATETIME())
            GROUP BY ConnectionId
        ) stats ON dc.ConnectionId = stats.ConnectionId
        ');

        PRINT '  ✓ Created monitoring.vw_DatabaseMonitoringSummary view';
    END
    ELSE
        PRINT '  ⚠ monitoring.vw_DatabaseMonitoringSummary view already exists';

    -- ============================================================================
    -- 6. CREATE STORED PROCEDURES FOR WORKER MANAGEMENT
    -- ============================================================================

    PRINT 'Creating stored procedures...';

    -- Procedure to register a new worker instance
    IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_RegisterWorkerInstance' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        EXEC('
        CREATE PROCEDURE monitoring.sp_RegisterWorkerInstance
            @WorkerName NVARCHAR(100),
            @MachineName NVARCHAR(100),
            @ProcessId INT,
            @Version NVARCHAR(50),
            @Environment NVARCHAR(50) = ''Production'',
            @Configuration NVARCHAR(MAX) = NULL,
            @Tags NVARCHAR(500) = NULL,
            @WorkerInstanceId UNIQUEIDENTIFIER OUTPUT
        AS
        BEGIN
            SET NOCOUNT ON;

            SET @WorkerInstanceId = NEWID();

            INSERT INTO monitoring.WorkerInstances (
                WorkerInstanceId, WorkerName, MachineName, ProcessId, Version,
                Environment, Status, Configuration, Tags
            )
            VALUES (
                @WorkerInstanceId, @WorkerName, @MachineName, @ProcessId, @Version,
                @Environment, ''Starting'', @Configuration, @Tags
            );

            RETURN 0;
        END
        ');

        PRINT '  ✓ Created monitoring.sp_RegisterWorkerInstance procedure';
    END
    ELSE
        PRINT '  ⚠ monitoring.sp_RegisterWorkerInstance procedure already exists';

    -- Procedure to update worker heartbeat
    IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateWorkerHeartbeat' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        EXEC('
        CREATE PROCEDURE monitoring.sp_UpdateWorkerHeartbeat
            @WorkerInstanceId UNIQUEIDENTIFIER,
            @Status NVARCHAR(20) = ''Running''
        AS
        BEGIN
            SET NOCOUNT ON;

            UPDATE monitoring.WorkerInstances
            SET
                LastHeartbeat = SYSUTCDATETIME(),
                Status = @Status,
                ModifiedDate = SYSUTCDATETIME()
            WHERE WorkerInstanceId = @WorkerInstanceId;

            RETURN @@ROWCOUNT;
        END
        ');

        PRINT '  ✓ Created monitoring.sp_UpdateWorkerHeartbeat procedure';
    END
    ELSE
        PRINT '  ⚠ monitoring.sp_UpdateWorkerHeartbeat procedure already exists';

    -- Procedure to cleanup old monitoring data
    IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanupMonitoringData' AND schema_id = SCHEMA_ID('monitoring'))
    BEGIN
        EXEC('
        CREATE PROCEDURE monitoring.sp_CleanupMonitoringData
            @RetentionDays INT = 30,
            @BatchSize INT = 1000
        AS
        BEGIN
            SET NOCOUNT ON;

            DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
            DECLARE @RowsDeleted INT = 1;
            DECLARE @TotalDeleted INT = 0;

            PRINT ''Starting cleanup of monitoring data older than '' + CAST(@RetentionDays AS NVARCHAR(10)) + '' days'';

            -- Cleanup DatabaseMonitoringResults
            WHILE @RowsDeleted > 0
            BEGIN
                DELETE TOP (@BatchSize) FROM monitoring.DatabaseMonitoringResults
                WHERE Timestamp < @CutoffDate;

                SET @RowsDeleted = @@ROWCOUNT;
                SET @TotalDeleted = @TotalDeleted + @RowsDeleted;

                IF @RowsDeleted > 0
                    WAITFOR DELAY ''00:00:01''; -- Small delay to avoid blocking
            END

            PRINT ''Deleted '' + CAST(@TotalDeleted AS NVARCHAR(10)) + '' monitoring result records'';

            -- Cleanup WorkerMetrics
            SET @RowsDeleted = 1;
            SET @TotalDeleted = 0;

            WHILE @RowsDeleted > 0
            BEGIN
                DELETE TOP (@BatchSize) FROM monitoring.WorkerMetrics
                WHERE Timestamp < @CutoffDate;

                SET @RowsDeleted = @@ROWCOUNT;
                SET @TotalDeleted = @TotalDeleted + @RowsDeleted;

                IF @RowsDeleted > 0
                    WAITFOR DELAY ''00:00:01'';
            END

            PRINT ''Deleted '' + CAST(@TotalDeleted AS NVARCHAR(10)) + '' worker metric records'';

            -- Cleanup completed WorkerJobs
            SET @RowsDeleted = 1;
            SET @TotalDeleted = 0;

            WHILE @RowsDeleted > 0
            BEGIN
                DELETE TOP (@BatchSize) FROM monitoring.WorkerJobs
                WHERE Status IN (''Completed'', ''Failed'', ''Cancelled'')
                  AND CreatedDate < @CutoffDate;

                SET @RowsDeleted = @@ROWCOUNT;
                SET @TotalDeleted = @TotalDeleted + @RowsDeleted;

                IF @RowsDeleted > 0
                    WAITFOR DELAY ''00:00:01'';
            END

            PRINT ''Deleted '' + CAST(@TotalDeleted AS NVARCHAR(10)) + '' completed job records'';

            RETURN 0;
        END
        ');

        PRINT '  ✓ Created monitoring.sp_CleanupMonitoringData procedure';
    END
    ELSE
        PRINT '  ⚠ monitoring.sp_CleanupMonitoringData procedure already exists';

    -- ============================================================================
    -- 7. INSERT INITIAL CONFIGURATION DATA
    -- ============================================================================

    PRINT 'Inserting initial configuration data...';

    -- Insert default database queries if they don''t exist
    IF NOT EXISTS (SELECT * FROM monitoring.DatabaseQueries WHERE QueryName = ''Connection Test'')
    BEGIN
        INSERT INTO monitoring.DatabaseQueries (
            QueryName, QueryType, SqlQuery, ResultType, Description
        ) VALUES (
            ''Connection Test'', ''HealthCheck'', ''SELECT 1 as Result'', ''Scalar'',
            ''Basic connection test query''
        );
        PRINT ''  ✓ Inserted default Connection Test query'';
    END

    IF NOT EXISTS (SELECT * FROM monitoring.DatabaseQueries WHERE QueryName = ''Database Size Check'')
    BEGIN
        INSERT INTO monitoring.DatabaseQueries (
            QueryName, QueryType, SqlQuery, ResultType, Description, WarningThreshold, CriticalThreshold
        ) VALUES (
            ''Database Size Check'', ''Performance'',
            ''SELECT SUM(size * 8.0 / 1024) as SizeMB FROM sys.master_files WHERE type = 0'',
            ''Scalar'', ''Check total database size in MB'', 10240, 20480
        );
        PRINT ''  ✓ Inserted default Database Size Check query'';
    END

    -- Insert default configuration values
    IF NOT EXISTS (SELECT * FROM monitoring.Config WHERE ConfigKey = ''MonitoringWorker.RetentionDays'')
    BEGIN
        INSERT INTO monitoring.Config (ConfigKey, ConfigValue, Description, Category)
        VALUES (''MonitoringWorker.RetentionDays'', ''30'', ''Number of days to retain monitoring data'', ''MonitoringWorker'');
        PRINT ''  ✓ Inserted MonitoringWorker.RetentionDays configuration'';
    END

    IF NOT EXISTS (SELECT * FROM monitoring.Config WHERE ConfigKey = ''MonitoringWorker.MaxConcurrentJobs'')
    BEGIN
        INSERT INTO monitoring.Config (ConfigKey, ConfigValue, Description, Category)
        VALUES (''MonitoringWorker.MaxConcurrentJobs'', ''10'', ''Maximum number of concurrent monitoring jobs'', ''MonitoringWorker'');
        PRINT ''  ✓ Inserted MonitoringWorker.MaxConcurrentJobs configuration'';
    END

    COMMIT TRANSACTION MonitoringWorkerSchemaUpdate;

    PRINT ''=================================================================================='';
    PRINT ''MonitoringWorker schema extension completed successfully!'';
    PRINT ''Tables created: WorkerInstances, WorkerJobs, DatabaseConnections, DatabaseQueries,'';
    PRINT ''                DatabaseMonitoringResults, WorkerMetrics'';
    PRINT ''Views created: vw_WorkerStatusOverview, vw_DatabaseMonitoringSummary'';
    PRINT ''Procedures created: sp_RegisterWorkerInstance, sp_UpdateWorkerHeartbeat, sp_CleanupMonitoringData'';
    PRINT ''Execution time: '' + CAST(DATEDIFF(MILLISECOND, @ExecutionStart, SYSUTCDATETIME()) AS NVARCHAR(10)) + '' ms'';
    PRINT ''=================================================================================='';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION MonitoringWorkerSchemaUpdate;

    SELECT
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    PRINT ''=================================================================================='';
    PRINT ''ERROR: Schema extension failed!'';
    PRINT ''Error Message: '' + @ErrorMessage;
    PRINT ''Error Severity: '' + CAST(@ErrorSeverity AS NVARCHAR(10));
    PRINT ''Error State: '' + CAST(@ErrorState AS NVARCHAR(10));
    PRINT ''=================================================================================='';

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

GO
