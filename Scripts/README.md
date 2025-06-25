# MonitoringWorker Scripts Directory

This directory contains various scripts and documentation for the MonitoringWorker project.

## Database Scripts

### Schema Management
- **`MonitoringWorker_Schema_Simple.sql`** - Production-ready database schema script
- **`Deploy-Schema.ps1`** - PowerShell script to deploy the database schema
- **`AnalyzeDatabase.ps1`** - Script to analyze existing database structure

### Testing and Utilities
- **`TestConnection.ps1`** - Test database connectivity
- **`Test-SqlExecution.ps1`** - Test SQL execution capabilities

### Legacy Files (Deprecated)
- **`MonitoringWorker_DatabaseSchema.sql`** - Original complex schema (use Simple version instead)

## Documentation

### Implementation Guides
- **`MonitoringWorker_DatabaseSchema_Documentation.md`** - Comprehensive database schema documentation
- **`MonitoringWorker_Implementation_Summary.md`** - Implementation summary and results

## Application Scripts

### Development
- **`start.ps1`** - Start the MonitoringWorker application
- **`cleanup.ps1`** - Clean up development artifacts

## Usage Instructions

### Database Setup
1. Review the database documentation in `MonitoringWorker_DatabaseSchema_Documentation.md`
2. Test connectivity using `TestConnection.ps1`
3. Deploy schema using `Deploy-Schema.ps1`
4. Verify deployment using `AnalyzeDatabase.ps1`

### Development Workflow
1. Use `start.ps1` to start the application
2. Use `cleanup.ps1` to clean up after development
3. Refer to documentation for API usage and testing

## Security Notes

- Database connection strings should never be committed to version control
- Use environment variables or secure configuration for sensitive data
- Test scripts are for development environments only

## Maintenance

- Keep documentation updated with any schema changes
- Test all scripts after major updates
- Archive deprecated scripts rather than deleting them
