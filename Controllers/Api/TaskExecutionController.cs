using Microsoft.AspNetCore.Mvc;
using FlightClub.Services;
using FlightClub.Services.TaskExecutors;

namespace FlightClub.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TaskExecutionController : ControllerBase
{
    private readonly ITaskTriggerService _taskTriggerService;
    private readonly ITaskExecutionService _taskExecutionService;
    private readonly ILogger<TaskExecutionController> _logger;

    public TaskExecutionController(
        ITaskTriggerService taskTriggerService,
        ITaskExecutionService taskExecutionService,
        ILogger<TaskExecutionController> logger)
    {
        _taskTriggerService = taskTriggerService;
        _taskExecutionService = taskExecutionService;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger execution of a specific task
    /// </summary>
    /// <param name="taskId">The task ID to execute</param>
    /// <returns>Task execution result</returns>
    [HttpPost("trigger/{taskId:int}")]
    [ProducesResponseType(typeof(TaskExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskExecutionResult>> TriggerTask(int taskId)
    {
        try
        {
            _logger.LogInformation("API trigger requested for task {TaskId}", taskId);
            
            var result = await _taskTriggerService.TriggerTaskAsync(taskId, HttpContext.RequestAborted);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering task {TaskId}", taskId);
            return StatusCode(500, new TaskExecutionResult
            {
                Success = false,
                Message = "Internal server error",
                ErrorDetails = ex.Message,
                ExecutionEndTime = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get execution statistics
    /// </summary>
    /// <returns>Task execution statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(TaskExecutionStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskExecutionStats>> GetExecutionStats()
    {
        try
        {
            var stats = await _taskTriggerService.GetExecutionStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution statistics");
            return StatusCode(500, "An error occurred while retrieving execution statistics");
        }
    }

    /// <summary>
    /// Get available task types that can be executed
    /// </summary>
    /// <returns>List of available task types</returns>
    [HttpGet("task-types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetAvailableTaskTypes()
    {
        try
        {
            var taskTypes = _taskExecutionService.GetAvailableTaskTypes();
            return Ok(taskTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available task types");
            return StatusCode(500, "An error occurred while retrieving task types");
        }
    }

    /// <summary>
    /// Get system health status for task execution
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            var stats = await _taskTriggerService.GetExecutionStatsAsync();
            var taskTypes = _taskExecutionService.GetAvailableTaskTypes();
            
            var health = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                TaskExecution = new
                {
                    AvailableExecutors = taskTypes.Count(),
                    RegisteredTaskTypes = taskTypes,
                    RunningTasks = stats.RunningTasks,
                    PendingTasks = stats.PendingTasks
                },
                Statistics = stats
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health status");
            return StatusCode(500, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}
