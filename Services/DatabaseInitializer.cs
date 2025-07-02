using Microsoft.EntityFrameworkCore;
using FlightClub.Data;

namespace FlightClub.Services;

/// <summary>
/// Service for database initialization and migration
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Initialize the database and run any necessary migrations
    /// </summary>
    public static async Task InitializeAsync(FlightClubDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Initializing database...");

            // Ensure database is created
            var created = await context.Database.EnsureCreatedAsync();
            
            if (created)
            {
                logger.LogInformation("Database created successfully at: {DatabasePath}", 
                    context.Database.GetConnectionString());
            }
            else
            {
                logger.LogInformation("Database already exists at: {DatabasePath}", 
                    context.Database.GetConnectionString());
            }

            // Check if we need to add any seed data
            var taskCount = await context.ScheduledTasks.CountAsync();
            logger.LogInformation("Database contains {TaskCount} scheduled tasks", taskCount);

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Get database health information
    /// </summary>
    public static async Task<DatabaseHealthInfo> GetHealthInfoAsync(FlightClubDbContext context)
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            var taskCount = canConnect ? await context.ScheduledTasks.CountAsync() : 0;
            
            return new DatabaseHealthInfo
            {
                CanConnect = canConnect,
                TaskCount = taskCount,
                ConnectionString = context.Database.GetConnectionString() ?? "Not configured",
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new DatabaseHealthInfo
            {
                CanConnect = false,
                TaskCount = 0,
                ConnectionString = context.Database.GetConnectionString() ?? "Not configured",
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Database health information
/// </summary>
public class DatabaseHealthInfo
{
    public bool CanConnect { get; set; }
    public int TaskCount { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}
