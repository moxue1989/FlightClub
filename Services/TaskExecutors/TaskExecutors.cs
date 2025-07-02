using System.Text.Json;

namespace FlightClub.Services.TaskExecutors;

/// <summary>
/// Generic executor for simple notification tasks
/// </summary>
public class NotificationTaskExecutor : ITaskExecutor
{
    private readonly ILogger<NotificationTaskExecutor> _logger;
    
    public string TaskType => "Notification";

    public NotificationTaskExecutor(ILogger<NotificationTaskExecutor> logger)
    {
        _logger = logger;
    }

    public bool CanExecute(TaskExecutionContext context)
    {
        return context.TaskType.Equals(TaskType, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<TaskExecutionResult> ExecuteAsync(TaskExecutionContext context)
    {
        var result = new TaskExecutionResult { ExecutionStartTime = DateTime.UtcNow };
        
        try
        {
            _logger.LogInformation("Executing notification task {TaskId}", context.TaskId);

            var parameters = ParseParameters(context.Parameters);
            var recipient = parameters?.Recipient ?? "Unknown";
            var message = parameters?.Message ?? context.TaskName;
            var type = parameters?.Type ?? "Email";

            _logger.LogInformation("Sending {Type} notification to {Recipient}: {Message}", 
                type, recipient, message);

            // Simulate notification sending
            await SimulateSendNotification(type, recipient, message, context.CancellationToken);

            var resultData = new Dictionary<string, object>
            {
                ["recipient"] = recipient,
                ["message"] = message,
                ["type"] = type,
                ["sentAt"] = DateTime.UtcNow
            };

            return TaskExecutionResult.CreateSuccess(
                $"{type} notification sent to {recipient}", 
                resultData);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Notification task {TaskId} was cancelled", context.TaskId);
            return TaskExecutionResult.CreateFailure("Notification task was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing notification task {TaskId}", context.TaskId);
            return TaskExecutionResult.CreateFailure("Notification failed", ex.Message);
        }
        finally
        {
            result.ExecutionEndTime = DateTime.UtcNow;
        }
    }

    private async Task SimulateSendNotification(string type, string recipient, string message, CancellationToken cancellationToken)
    {
        // Simulate notification sending delay
        var delay = type.ToLower() switch
        {
            "email" => 1000,
            "sms" => 500,
            "push" => 200,
            _ => 1000
        };

        await Task.Delay(delay, cancellationToken);
        _logger.LogInformation("Notification sent successfully");
    }

    private NotificationParameters? ParseParameters(string? parametersJson)
    {
        if (string.IsNullOrEmpty(parametersJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<NotificationParameters>(parametersJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse notification parameters: {Parameters}", parametersJson);
            return null;
        }
    }

    private class NotificationParameters
    {
        public string? Recipient { get; set; }
        public string? Message { get; set; }
        public string? Type { get; set; }
    }
}
