using FlightClub.Services.TaskExecutors;

namespace FlightClub.Services;

/// <summary>
/// Background service that monitors and executes scheduled tasks
/// </summary>
public class TaskSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskSchedulerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds
    private readonly SemaphoreSlim _executionSemaphore;
    private readonly int _maxConcurrentTasks = 5; // Maximum concurrent task executions

    public TaskSchedulerService(IServiceProvider serviceProvider, ILogger<TaskSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _executionSemaphore = new SemaphoreSlim(_maxConcurrentTasks, _maxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledTasks(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in task scheduler main loop");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
        }

        _logger.LogInformation("Task Scheduler Service stopped");
    }

    private async Task ProcessScheduledTasks(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduledTaskService = scope.ServiceProvider.GetRequiredService<IScheduledTaskService>();
        var taskExecutionService = scope.ServiceProvider.GetRequiredService<ITaskExecutionService>();

        try
        {
            // Get all pending tasks
            var pendingTasks = await scheduledTaskService.GetTasksAsync("Pending");
            var dueTasks = pendingTasks.Where(t => t.ScheduledTime <= DateTime.UtcNow).ToList();

            if (dueTasks.Any())
            {
                _logger.LogInformation("Found {Count} tasks due for execution", dueTasks.Count);

                // Execute due tasks concurrently (with limit)
                var executionTasks = dueTasks.Select(task => ExecuteTaskSafely(task.Id, taskExecutionService, cancellationToken));
                await Task.WhenAll(executionTasks);
            }
            else
            {
                _logger.LogDebug("No tasks due for execution at {Time}", DateTime.UtcNow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled tasks");
        }
    }

    private async Task ExecuteTaskSafely(int taskId, ITaskExecutionService taskExecutionService, CancellationToken cancellationToken)
    {
        // Wait for available execution slot
        await _executionSemaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting background execution of task {TaskId}", taskId);
            
            using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Set a reasonable timeout for task execution (10 minutes)
            taskCts.CancelAfter(TimeSpan.FromMinutes(10));

            var result = await taskExecutionService.ExecuteTaskAsync(taskId, taskCts.Token);
            
            if (result.Success)
            {
                _logger.LogInformation("Task {TaskId} executed successfully in {Duration}ms", 
                    taskId, result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("Task {TaskId} execution failed: {Message}", 
                    taskId, result.Message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Task {TaskId} execution cancelled due to service shutdown", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing task {TaskId}", taskId);
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    public override void Dispose()
    {
        _executionSemaphore?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Service for manually triggering task execution
/// </summary>
public interface ITaskTriggerService
{
    /// <summary>
    /// Manually trigger execution of a specific task
    /// </summary>
    Task<TaskExecutionResult> TriggerTaskAsync(int taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get execution statistics
    /// </summary>
    Task<TaskExecutionStats> GetExecutionStatsAsync();
}

public class TaskTriggerService : ITaskTriggerService
{
    private readonly ITaskExecutionService _taskExecutionService;
    private readonly IScheduledTaskService _scheduledTaskService;
    private readonly ILogger<TaskTriggerService> _logger;

    public TaskTriggerService(
        ITaskExecutionService taskExecutionService,
        IScheduledTaskService scheduledTaskService,
        ILogger<TaskTriggerService> logger)
    {
        _taskExecutionService = taskExecutionService;
        _scheduledTaskService = scheduledTaskService;
        _logger = logger;
    }

    public async Task<TaskExecutionResult> TriggerTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual trigger requested for task {TaskId}", taskId);
        
        var result = await _taskExecutionService.ExecuteTaskAsync(taskId, cancellationToken);
        
        _logger.LogInformation("Manual trigger completed for task {TaskId}. Success: {Success}", 
            taskId, result.Success);
        
        return result;
    }

    public async Task<TaskExecutionStats> GetExecutionStatsAsync()
    {
        var allTasks = await _scheduledTaskService.GetTasksAsync();
        
        return new TaskExecutionStats
        {
            TotalTasks = allTasks.Count(),
            PendingTasks = allTasks.Count(t => t.Status == "Pending"),
            RunningTasks = allTasks.Count(t => t.Status == "Running"),
            CompletedTasks = allTasks.Count(t => t.Status == "Completed"),
            FailedTasks = allTasks.Count(t => t.Status == "Failed"),
            CancelledTasks = allTasks.Count(t => t.Status == "Cancelled"),
            TasksByType = allTasks.GroupBy(t => t.TaskType)
                .ToDictionary(g => g.Key, g => g.Count()),
            LastUpdated = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Task execution statistics
/// </summary>
public class TaskExecutionStats
{
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int RunningTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int CancelledTasks { get; set; }
    public Dictionary<string, int> TasksByType { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
