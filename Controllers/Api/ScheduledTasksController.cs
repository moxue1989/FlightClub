using Microsoft.AspNetCore.Mvc;
using FlightClub.Models.Api;
using FlightClub.Services;
using FlightClub.Data;
using System.ComponentModel.DataAnnotations;

namespace FlightClub.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ScheduledTasksController : ControllerBase
{
    private readonly IScheduledTaskService _scheduledTaskService;
    private readonly FlightClubDbContext _context;
    private readonly ILogger<ScheduledTasksController> _logger;

    public ScheduledTasksController(
        IScheduledTaskService scheduledTaskService,
        FlightClubDbContext context,
        ILogger<ScheduledTasksController> logger)
    {
        _scheduledTaskService = scheduledTaskService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new scheduled task
    /// </summary>
    /// <param name="request">The scheduled task creation request</param>
    /// <returns>The created scheduled task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ScheduledTaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduledTaskResponse>> CreateTask([FromBody] CreateScheduledTaskRequest request)
    {
        try
        {
            _logger.LogInformation("Received request to create scheduled task: {TaskName}", request.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that scheduled time is in the future
            if (request.ScheduledTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(request.ScheduledTime), "Scheduled time must be in the future");
                return BadRequest(ModelState);
            }

            var result = await _scheduledTaskService.CreateTaskAsync(request);
            
            return CreatedAtAction(
                nameof(GetTask),
                new { id = result.Id },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scheduled task");
            return StatusCode(500, "An error occurred while creating the scheduled task");
        }
    }

    /// <summary>
    /// Gets a scheduled task by ID
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <returns>The scheduled task</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ScheduledTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledTaskResponse>> GetTask(int id)
    {
        try
        {
            var task = await _scheduledTaskService.GetTaskAsync(id);
            
            if (task == null)
            {
                return NotFound($"Scheduled task with ID {id} not found");
            }

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled task {TaskId}", id);
            return StatusCode(500, "An error occurred while retrieving the scheduled task");
        }
    }

    /// <summary>
    /// Gets all scheduled tasks with optional filtering
    /// </summary>
    /// <param name="status">Filter by task status</param>
    /// <param name="taskType">Filter by task type</param>
    /// <returns>List of scheduled tasks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ScheduledTaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScheduledTaskResponse>>> GetTasks(
        [FromQuery] string? status = null,
        [FromQuery] string? taskType = null)
    {
        try
        {
            var tasks = await _scheduledTaskService.GetTasksAsync(status, taskType);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled tasks");
            return StatusCode(500, "An error occurred while retrieving scheduled tasks");
        }
    }

    /// <summary>
    /// Updates a scheduled task
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="request">The update request</param>
    /// <returns>The updated scheduled task</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ScheduledTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledTaskResponse>> UpdateTask(
        int id, 
        [FromBody] UpdateScheduledTaskRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate scheduled time if provided
            if (request.ScheduledTime.HasValue && request.ScheduledTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(request.ScheduledTime), "Scheduled time must be in the future");
                return BadRequest(ModelState);
            }

            var result = await _scheduledTaskService.UpdateTaskAsync(id, request);
            
            if (result == null)
            {
                return NotFound($"Scheduled task with ID {id} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled task {TaskId}", id);
            return StatusCode(500, "An error occurred while updating the scheduled task");
        }
    }

    /// <summary>
    /// Updates the status of a scheduled task
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="status">The new status</param>
    /// <returns>The updated scheduled task</returns>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ScheduledTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledTaskResponse>> UpdateTaskStatus(
        int id, 
        [FromBody] [Required] string status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest("Status cannot be empty");
            }

            var validStatuses = new[] { "Pending", "Running", "Completed", "Failed", "Cancelled" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest($"Invalid status. Valid statuses are: {string.Join(", ", validStatuses)}");
            }

            var result = await _scheduledTaskService.UpdateTaskStatusAsync(id, status);
            
            if (result == null)
            {
                return NotFound($"Scheduled task with ID {id} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for scheduled task {TaskId}", id);
            return StatusCode(500, "An error occurred while updating the task status");
        }
    }

    /// <summary>
    /// Deletes a scheduled task
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(int id)
    {
        try
        {
            var success = await _scheduledTaskService.DeleteTaskAsync(id);
            
            if (!success)
            {
                return NotFound($"Scheduled task with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scheduled task {TaskId}", id);
            return StatusCode(500, "An error occurred while deleting the scheduled task");
        }
    }

    /// <summary>
    /// Gets database health information
    /// </summary>
    /// <returns>Database health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(DatabaseHealthInfo), StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseHealthInfo>> GetHealth()
    {
        try
        {
            var healthInfo = await DatabaseInitializer.GetHealthInfoAsync(_context);
            return Ok(healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database health");
            return StatusCode(500, "An error occurred while checking database health");
        }
    }
}
