using FlightClub.Models.Api;
using FlightClub.Services.TaskExecutors;
using Microsoft.Build.Framework;

namespace FlightClub.Services;

/// <summary>
/// Service for executing scheduled tasks
/// </summary>
public interface ITaskExecutionService
{
    /// <summary>
    /// Execute a specific task by ID
    /// </summary>
    Task<TaskExecutionResult> ExecuteTaskAsync(ScheduledTaskResponse task, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Register a task executor
    /// </summary>
    void RegisterExecutor(ITaskExecutor executor);
    
    /// <summary>
    /// Get available task types
    /// </summary>
    IEnumerable<string> GetAvailableTaskTypes();
}

public class TaskExecutionService : ITaskExecutionService
{
    private readonly ILogger<TaskExecutionService> _logger;
    private readonly Dictionary<string, ITaskExecutor> _executors;

    public TaskExecutionService(
        ILogger<TaskExecutionService> logger,
        IEnumerable<ITaskExecutor> executors)
    {
        _logger = logger;
        _executors = new Dictionary<string, ITaskExecutor>(StringComparer.OrdinalIgnoreCase);
        
        // Register all provided executors
        foreach (var executor in executors)
        {
            RegisterExecutor(executor);
        }
    }

    public async Task<TaskExecutionResult> ExecuteTaskAsync(ScheduledTaskResponse task, CancellationToken cancellationToken = default)
    {
        
        // Get the task details
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found", task);
            return TaskExecutionResult.CreateFailure($"Task not found");
        }
        var taskId = task.Id;
        try
        {
            _logger.LogInformation("Starting execution of task {TaskId}", task.Id);

            // Check if task is in a valid state for execution
            if (task.Status != "Pending")
            {
                _logger.LogWarning("Task {TaskId} is not in pending state. Current status: {Status}", 
                    taskId, task.Status);
                return TaskExecutionResult.CreateFailure(
                    $"Task {taskId} is not in pending state. Current status: {task.Status}");
            }

            // Find appropriate executor
            if (!_executors.TryGetValue(task.TaskType, out var executor))
            {
                _logger.LogError("No executor found for task type {TaskType}", task.TaskType);
                return TaskExecutionResult.CreateFailure(
                    $"No executor available for task type: {task.TaskType}");
            }

            // Create execution context
            var context = new TaskExecutionContext
            {
                TaskId = task.Id,
                TaskName = task.Name,
                TaskType = task.TaskType,
                Parameters = task.Parameters,
                ScheduledTime = task.ScheduledTime,
                CreatedBy = task.CreatedBy,
                CancellationToken = cancellationToken
            };

            // Verify executor can handle this task
            if (!executor.CanExecute(context))
            {
                _logger.LogError("Executor for {TaskType} cannot handle task {TaskId}", task.TaskType, taskId);
                return TaskExecutionResult.CreateFailure(
                    $"Executor for {task.TaskType} cannot handle this task");
            }

            // Update task status to running
            _logger.LogInformation("Task {TaskId} status updated to Running", taskId);

            // Execute the task
            var result = await executor.ExecuteAsync(context);

            _logger.LogInformation("Task {TaskId} execution completed. Success: {Success}, Duration: {Duration}ms", 
                taskId, result.Success, result.Duration.TotalMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Task {TaskId} execution was cancelled", taskId);
            return TaskExecutionResult.CreateFailure("Task execution was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing task {TaskId}", taskId);
            return TaskExecutionResult.CreateFailure("Unexpected error during task execution", ex.Message);
        }
    }

    public void RegisterExecutor(ITaskExecutor executor)
    {
        if (executor == null)
            throw new ArgumentNullException(nameof(executor));

        _executors[executor.TaskType] = executor;
        _logger.LogInformation("Registered executor for task type: {TaskType}", executor.TaskType);
    }

    public IEnumerable<string> GetAvailableTaskTypes()
    {
        return _executors.Keys.OrderBy(k => k);
    }
}
