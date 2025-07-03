using FlightClub.Models.Api;
using FlightClub.Data;
using Microsoft.EntityFrameworkCore;

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
    private readonly FlightClubDbContext _context;

    public ScheduledTaskService(
        ILogger<ScheduledTaskService> logger, 
        FlightClubDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ScheduledTaskResponse> CreateTaskAsync(CreateScheduledTaskRequest request)
    {
        _logger.LogInformation("Creating new scheduled task: {TaskName}", request.Name);

        var task = new ScheduledTask
        {
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

        _context.ScheduledTasks.Add(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created scheduled task with ID: {TaskId}", task.Id);

        return MapToResponse(task);
    }

    public async Task<ScheduledTaskResponse?> GetTaskAsync(int id)
    {
        var task = await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id);
        
        return task != null ? MapToResponse(task) : null;
    }

    public async Task<IEnumerable<ScheduledTaskResponse>> GetTasksAsync(string? status = null, string? taskType = null)
    {
        var query = _context.ScheduledTasks.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(taskType))
        {
            query = query.Where(t => t.TaskType == taskType);
        }

        var tasks = await query
            .OrderBy(t => t.ScheduledTime)
            .ToListAsync();

        return tasks.Select(MapToResponse);
    }

    public async Task<ScheduledTaskResponse?> UpdateTaskAsync(int id, UpdateScheduledTaskRequest request)
    {
        var task = await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id);
        
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

        await _context.SaveChangesAsync();

        return MapToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        var task = await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (task == null)
            return false;

        _logger.LogInformation("Deleting scheduled task: {TaskId}", id);
        
        _context.ScheduledTasks.Remove(task);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<ScheduledTaskResponse?> UpdateTaskStatusAsync(int id, string status)
    {
        var task = await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (task == null)
            return null;

        _logger.LogInformation("Updating task {TaskId} status to: {Status}", id, status);
        
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(task);
    }

    private ScheduledTaskResponse MapToResponse(ScheduledTask task)
    {
        return new ScheduledTaskResponse
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            ScheduledTime = DateTime.SpecifyKind(task.ScheduledTime, DateTimeKind.Utc),
            TaskType = task.TaskType,
            Parameters = task.Parameters, // No obfuscation - return original parameters
            Status = task.Status,
            Priority = task.Priority,
            CreatedAt = DateTime.SpecifyKind(task.CreatedAt, DateTimeKind.Utc),
            UpdatedAt = task.UpdatedAt.HasValue ? DateTime.SpecifyKind(task.UpdatedAt.Value, DateTimeKind.Utc) : null,
            CreatedBy = task.CreatedBy
        };
    }
}
