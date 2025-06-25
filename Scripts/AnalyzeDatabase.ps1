# PowerShell script to analyze the PopAI database schema
param(
    [string]$ConnectionString = "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=PWUi^g6~lxD;TrustServerCertificate=true;"
)

# Import SQL Server module if available
try {
    Import-Module SqlServer -ErrorAction SilentlyContinue
} catch {
    Write-Warning "SqlServer module not available. Using System.Data.SqlClient instead."
}

function Get-DatabaseSchema {
    param([string]$ConnectionString)
    
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    
    try {
        $connection.Open()
        Write-Host "Successfully connected to database: $($connection.Database)" -ForegroundColor Green
        Write-Host "Server version: $($connection.ServerVersion)" -ForegroundColor Green
        
        # Get all tables with their schemas
        $tablesQuery = @"
SELECT 
    s.name AS SchemaName,
    t.name AS TableName,
    t.object_id,
    t.create_date,
    t.modify_date
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
ORDER BY s.name, t.name
"@
        
        $command = New-Object System.Data.SqlClient.SqlCommand($tablesQuery, $connection)
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
        $tables = New-Object System.Data.DataTable
        $adapter.Fill($tables) | Out-Null
        
        Write-Host "`nFound $($tables.Rows.Count) tables:" -ForegroundColor Yellow
        
        $schemaInfo = @{}
        
        foreach ($table in $tables.Rows) {
            $schemaName = $table.SchemaName
            $tableName = $table.TableName
            $fullTableName = "$schemaName.$tableName"
            
            Write-Host "  - $fullTableName" -ForegroundColor Cyan
            
            # Get columns for this table
            $columnsQuery = @"
SELECT 
    c.column_name,
    c.data_type,
    c.character_maximum_length,
    c.numeric_precision,
    c.numeric_scale,
    c.is_nullable,
    c.column_default,
    CASE WHEN pk.column_name IS NOT NULL THEN 'YES' ELSE 'NO' END AS is_primary_key
FROM information_schema.columns c
LEFT JOIN (
    SELECT ku.table_name, ku.column_name
    FROM information_schema.table_constraints tc
    INNER JOIN information_schema.key_column_usage ku
        ON tc.constraint_name = ku.constraint_name
    WHERE tc.constraint_type = 'PRIMARY KEY'
) pk ON c.table_name = pk.table_name AND c.column_name = pk.column_name
WHERE c.table_schema = '$schemaName' AND c.table_name = '$tableName'
ORDER BY c.ordinal_position
"@
            
            $columnCommand = New-Object System.Data.SqlClient.SqlCommand($columnsQuery, $connection)
            $columnAdapter = New-Object System.Data.SqlClient.SqlDataAdapter($columnCommand)
            $columns = New-Object System.Data.DataTable
            $columnAdapter.Fill($columns) | Out-Null
            
            $schemaInfo[$fullTableName] = @{
                Schema = $schemaName
                Table = $tableName
                Columns = $columns.Rows
                CreateDate = $table.create_date
                ModifyDate = $table.modify_date
            }
        }
        
        return $schemaInfo
        
    } catch {
        Write-Error "Error connecting to database: $($_.Exception.Message)"
        return $null
    } finally {
        if ($connection.State -eq 'Open') {
            $connection.Close()
        }
    }
}

function Export-SchemaToFile {
    param(
        [hashtable]$SchemaInfo,
        [string]$OutputPath = "DatabaseSchema.txt"
    )
    
    $output = @()
    $output += "PopAI Database Schema Analysis"
    $output += "Generated: $(Get-Date)"
    $output += "=" * 50
    $output += ""
    
    foreach ($tableKey in ($SchemaInfo.Keys | Sort-Object)) {
        $tableInfo = $SchemaInfo[$tableKey]
        $output += "TABLE: $tableKey"
        $output += "Created: $($tableInfo.CreateDate)"
        $output += "Modified: $($tableInfo.ModifyDate)"
        $output += "-" * 30
        
        foreach ($column in $tableInfo.Columns) {
            $nullable = if ($column.is_nullable -eq "YES") { "NULL" } else { "NOT NULL" }
            $pk = if ($column.is_primary_key -eq "YES") { " [PK]" } else { "" }
            $length = if ($column.character_maximum_length) { "($($column.character_maximum_length))" } else { "" }
            $precision = if ($column.numeric_precision -and $column.numeric_scale) { "($($column.numeric_precision),$($column.numeric_scale))" } else { "" }
            
            $output += "  $($column.column_name) $($column.data_type)$length$precision $nullable$pk"
            if ($column.column_default) {
                $output += "    DEFAULT: $($column.column_default)"
            }
        }
        $output += ""
    }
    
    $output | Out-File -FilePath $OutputPath -Encoding UTF8
    Write-Host "Schema exported to: $OutputPath" -ForegroundColor Green
}

# Main execution
Write-Host "Analyzing PopAI database schema..." -ForegroundColor Yellow
$schema = Get-DatabaseSchema -ConnectionString $ConnectionString

if ($schema) {
    Export-SchemaToFile -SchemaInfo $schema -OutputPath "PopAI_DatabaseSchema.txt"
    
    # Display summary
    Write-Host "`nSchema Summary:" -ForegroundColor Yellow
    Write-Host "Total tables: $($schema.Count)" -ForegroundColor Green
    
    $schemaGroups = $schema.Values | Group-Object Schema
    foreach ($group in $schemaGroups) {
        Write-Host "  $($group.Name) schema: $($group.Count) tables" -ForegroundColor Cyan
    }
} else {
    Write-Error "Failed to retrieve database schema"
}
