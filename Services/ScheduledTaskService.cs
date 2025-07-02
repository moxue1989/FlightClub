using FlightClub.Models.Api;

namespace FlightClub.Services;

public interface IScheduledTaskService
{
    Task<ScheduledTaskResponse> CreateTaskAsync(CreateScheduledTaskRequest request);
    Task<ScheduledTaskResponse?> GetTaskAsync(int id);
    Task<IEnumerable<ScheduledTaskResponse>> GetTasksAsync(string? status = null, string? taskType = null);
    Task<ScheduledTaskResponse?> UpdateTaskAsync(int id, UpdateScheduledTaskRequest request);
    Task<bool> DeleteTaskAsync(int id);
    Task<ScheduledTaskResponse?> UpdateTaskStatusAsync(int id, string status);
}

public class ScheduledTaskService : IScheduledTaskService
{
    private readonly ILogger<ScheduledTaskService> _logger;
    private static readonly List<ScheduledTask> _tasks = new();
    private static int _nextId = 1;

    public ScheduledTaskService(ILogger<ScheduledTaskService> logger)
    {
        _logger = logger;
    }

    public async Task<ScheduledTaskResponse> CreateTaskAsync(CreateScheduledTaskRequest request)
    {
        _logger.LogInformation("Creating new scheduled task: {TaskName}", request.Name);

        var task = new ScheduledTask
        {
            Id = _nextId++,
            Name = request.Name,
            Description = request.Description,
            ScheduledTime = request.ScheduledTime.ToUniversalTime(),
            TaskType = request.TaskType,
            Parameters = request.Parameters,
            Priority = request.Priority,
            CreatedBy = request.CreatedBy,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _tasks.Add(task);

        _logger.LogInformation("Created scheduled task with ID: {TaskId}", task.Id);

        return await Task.FromResult(MapToResponse(task));
    }

    public async Task<ScheduledTaskResponse?> GetTaskAsync(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        return await Task.FromResult(task != null ? MapToResponse(task) : null);
    }

    public async Task<IEnumerable<ScheduledTaskResponse>> GetTasksAsync(string? status = null, string? taskType = null)
    {
        var query = _tasks.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(taskType))
        {
            query = query.Where(t => t.TaskType.Equals(taskType, StringComparison.OrdinalIgnoreCase));
        }

        var results = query.OrderBy(t => t.ScheduledTime).Select(MapToResponse);
        return await Task.FromResult(results);
    }

    public async Task<ScheduledTaskResponse?> UpdateTaskAsync(int id, UpdateScheduledTaskRequest request)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return null;

        _logger.LogInformation("Updating scheduled task: {TaskId}", id);

        if (!string.IsNullOrEmpty(request.Name))
            task.Name = request.Name;
        
        if (request.Description != null)
            task.Description = request.Description;
        
        if (request.ScheduledTime.HasValue)
            task.ScheduledTime = request.ScheduledTime.Value.ToUniversalTime();
        
        if (!string.IsNullOrEmpty(request.TaskType))
            task.TaskType = request.TaskType;
        
        if (request.Parameters != null)
            task.Parameters = request.Parameters;
        
        if (!string.IsNullOrEmpty(request.Status))
            task.Status = request.Status;
        
        if (request.Priority.HasValue)
            task.Priority = request.Priority.Value;

        task.UpdatedAt = DateTime.UtcNow;

        return await Task.FromResult(MapToResponse(task));
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return false;

        _logger.LogInformation("Deleting scheduled task: {TaskId}", id);
        _tasks.Remove(task);
        return await Task.FromResult(true);
    }

    public async Task<ScheduledTaskResponse?> UpdateTaskStatusAsync(int id, string status)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return null;

        _logger.LogInformation("Updating task {TaskId} status to: {Status}", id, status);
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;

        return await Task.FromResult(MapToResponse(task));
    }

    private static ScheduledTaskResponse MapToResponse(ScheduledTask task)
    {
        return new ScheduledTaskResponse
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            ScheduledTime = task.ScheduledTime,
            TaskType = task.TaskType,
            Parameters = ObfuscateParametersIfNeeded(task.Parameters, task.TaskType),
            Status = task.Status,
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CreatedBy = task.CreatedBy
        };
    }
    
    private static string? ObfuscateParametersIfNeeded(string? parameters, string taskType)
    {
        // Only obfuscate parameters for ReserveBuntzen tasks
        if (string.IsNullOrEmpty(parameters) || !taskType.Equals("ReserveBuntzen", StringComparison.OrdinalIgnoreCase))
        {
            return parameters;
        }
        
        try
        {
            // Parse the JSON parameters
            var jsonDoc = System.Text.Json.JsonDocument.Parse(parameters);
            var root = jsonDoc.RootElement;
            
            // Create a new JSON object with obfuscated AuthToken
            var obfuscatedParams = new Dictionary<string, object>();
            bool tokenObfuscated = false;
            
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.Equals("AuthToken", StringComparison.OrdinalIgnoreCase))
                {
                    // Obfuscate the auth token
                    var tokenValue = property.Value.GetString();
                    obfuscatedParams[property.Name] = ObfuscateToken(tokenValue);
                    tokenObfuscated = true;
                }
                else
                {
                    // Keep other parameters as-is
                    object paramValue = property.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => property.Value.GetString() ?? "",
                        System.Text.Json.JsonValueKind.Number => property.Value.TryGetInt32(out var intVal) ? intVal : property.Value.GetDouble(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Null => null!,
                        _ => property.Value.GetRawText()
                    };
                    obfuscatedParams[property.Name] = paramValue;
                }
            }
            
            // Serialize back to JSON
            var result = System.Text.Json.JsonSerializer.Serialize(obfuscatedParams);
            
            // Add a comment to indicate obfuscation was applied (for debugging)
            if (tokenObfuscated)
            {
                // Note: This is just for internal tracking, not visible in the JSON
                System.Diagnostics.Debug.WriteLine($"AuthToken obfuscated in ReserveBuntzen task parameters");
            }
            
            return result;
        }
        catch (System.Text.Json.JsonException)
        {
            // If JSON parsing fails, return original parameters
            return parameters;
        }
    }
    
    private static string ObfuscateToken(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length <= 8)
            return "[REDACTED]";
        
        return $"{token[..4]}...{token[^4..]}";
    }
}
