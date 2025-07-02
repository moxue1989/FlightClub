namespace FlightClub.Services.TaskExecutors;

/// <summary>
/// Represents a task execution context
/// </summary>
public class TaskExecutionContext
{
    public int TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string? CreatedBy { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// Represents the result of a task execution
/// </summary>
public class TaskExecutionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorDetails { get; set; }
    public Dictionary<string, object>? ResultData { get; set; }
    public DateTime ExecutionStartTime { get; set; }
    public DateTime ExecutionEndTime { get; set; }
    public TimeSpan Duration => ExecutionEndTime - ExecutionStartTime;

    public static TaskExecutionResult CreateSuccess(string? message = null, Dictionary<string, object>? resultData = null)
    {
        return new TaskExecutionResult
        {
            Success = true,
            Message = message,
            ResultData = resultData,
            ExecutionEndTime = DateTime.UtcNow
        };
    }

    public static TaskExecutionResult CreateFailure(string message, string? errorDetails = null)
    {
        return new TaskExecutionResult
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails,
            ExecutionEndTime = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Interface for task executors
/// </summary>
public interface ITaskExecutor
{
    /// <summary>
    /// The task type this executor handles
    /// </summary>
    string TaskType { get; }
    
    /// <summary>
    /// Execute the task
    /// </summary>
    Task<TaskExecutionResult> ExecuteAsync(TaskExecutionContext context);
    
    /// <summary>
    /// Validate if the executor can handle the given parameters
    /// </summary>
    bool CanExecute(TaskExecutionContext context);
}
