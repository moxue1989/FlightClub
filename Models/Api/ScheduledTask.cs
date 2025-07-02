using System.ComponentModel.DataAnnotations;

namespace FlightClub.Models.Api;

public class ScheduledTask
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public DateTime ScheduledTime { get; set; }
    
    [Required]
    [StringLength(50)]
    public string TaskType { get; set; } = string.Empty;
    
    public string? Parameters { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";
    
    public int Priority { get; set; } = 1;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
}

public class CreateScheduledTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public DateTime ScheduledTime { get; set; }
    
    [Required]
    [StringLength(50)]
    public string TaskType { get; set; } = string.Empty;
    
    public string? Parameters { get; set; }
    
    public int Priority { get; set; } = 1;
    
    public string? CreatedBy { get; set; }
}

public class UpdateScheduledTaskRequest
{
    [StringLength(200)]
    public string? Name { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public DateTime? ScheduledTime { get; set; }
    
    [StringLength(50)]
    public string? TaskType { get; set; }
    
    public string? Parameters { get; set; }
    
    [StringLength(20)]
    public string? Status { get; set; }
    
    public int? Priority { get; set; }
}

public class ScheduledTaskResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
