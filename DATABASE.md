# FlightClub Database Setup

This document describes the SQLite database setup for FlightClub scheduled tasks.

## Database Location

The SQLite database is stored at:
```
wwwroot/database.db
```

## Database Schema

The database contains the following tables:

### ScheduledTasks
- **Id** (INTEGER PRIMARY KEY) - Auto-incrementing task ID
- **Name** (VARCHAR(200)) - Task name
- **Description** (VARCHAR(1000)) - Optional task description
- **ScheduledTime** (DATETIME) - When the task should be executed (UTC)
- **TaskType** (VARCHAR(50)) - Type of task (e.g., "ReserveBuntzen")
- **Parameters** (TEXT) - JSON parameters for the task
- **Status** (VARCHAR(20)) - Task status (Pending, Running, Completed, Failed, Cancelled)
- **Priority** (INTEGER) - Task priority (1-3, where 1 is highest)
- **CreatedAt** (DATETIME) - When the task was created (UTC)
- **UpdatedAt** (DATETIME) - When the task was last updated (UTC)
- **CreatedBy** (VARCHAR(100)) - Who created the task

## Indexes

The following indexes are created for performance:
- `IX_ScheduledTasks_Status` - Index on Status column
- `IX_ScheduledTasks_TaskType` - Index on TaskType column
- `IX_ScheduledTasks_ScheduledTime` - Index on ScheduledTime column
- `IX_ScheduledTasks_Status_ScheduledTime` - Composite index on Status and ScheduledTime

## Database Initialization

The database is automatically created when the application starts if it doesn't exist. The initialization process:

1. Ensures the `wwwroot` directory exists
2. Creates the database file if it doesn't exist
3. Creates all necessary tables and indexes
4. Logs the initialization status

## Health Check

You can check the database health by calling:
```
GET /api/scheduledtasks/health
```

This endpoint returns:
- **CanConnect** - Whether the database connection is working
- **TaskCount** - Number of tasks in the database
- **ConnectionString** - Database connection string
- **LastChecked** - When the health check was performed
- **ErrorMessage** - Any error message if the check failed

## Connection String

The connection string is configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=wwwroot/database.db"
  }
}
```

## Security Considerations

1. **Database File**: The database file is stored in `wwwroot` but is excluded from git via `.gitignore`
2. **Parameter Obfuscation**: Sensitive parameters (like AuthTokens) are automatically obfuscated in API responses
3. **Backups**: Consider implementing regular backups of the database file for production use

## Migration

Since this uses SQLite with Entity Framework Core, the database schema is managed through code-first approach. Schema changes are handled by:

1. Updating the `ScheduledTask` model
2. Updating the `FlightClubDbContext` configuration
3. The database will be automatically updated on application startup

## Troubleshooting

### Database Lock Issues
If you encounter database lock errors, ensure:
1. No other instances of the application are running
2. The database file is not open in any SQLite tools
3. The application has write permissions to the `wwwroot` directory

### Performance Issues
If you experience slow queries:
1. Check the database health endpoint for task count
2. Consider archiving old completed tasks
3. Verify indexes are being used properly

### Connection Issues
If the database won't connect:
1. Check that the `wwwroot` directory exists and is writable
2. Verify the connection string in `appsettings.json`
3. Check application logs for initialization errors
