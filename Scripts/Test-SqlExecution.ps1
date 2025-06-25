# Test SQL execution
param(
    [string]$ConnectionString = "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=PWUi^g6~lxD;TrustServerCertificate=true;"
)

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()
    
    Write-Host "Connected successfully" -ForegroundColor Green
    
    # Test simple query first
    $testQuery = "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('monitoring')"
    $command = New-Object System.Data.SqlClient.SqlCommand($testQuery, $connection)
    $result = $command.ExecuteScalar()
    
    Write-Host "Found $result tables in monitoring schema" -ForegroundColor Green
    
    # Test creating a simple table
    $createTableQuery = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestTable' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    CREATE TABLE monitoring.TestTable (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Test table created successfully';
END
ELSE
    PRINT 'Test table already exists';
"@
    
    Write-Host "Executing test table creation..." -ForegroundColor Yellow
    $command = New-Object System.Data.SqlClient.SqlCommand($createTableQuery, $connection)
    $command.ExecuteNonQuery()
    
    Write-Host "Test completed successfully" -ForegroundColor Green
    
    $connection.Close()
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
}
