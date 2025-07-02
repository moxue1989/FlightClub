using System.Text.Json;

namespace FlightClub.Services.TaskExecutors;

/// <summary>
/// Executor for Buntzen Lake reservation tasks using YodelPass API
/// </summary>
public class ReserveBuntzenExecutor : ITaskExecutor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReserveBuntzenExecutor> _logger;
    
    public string TaskType => "ReserveBuntzen";

    public ReserveBuntzenExecutor(ILogger<ReserveBuntzenExecutor> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
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
            _logger.LogInformation("Executing Buntzen Lake reservation task {TaskId}", context.TaskId);

            // Parse parameters
            var parameters = ParseParameters(context.Parameters);
            var reservationDate = parameters?.Date ?? DateTime.Today.AddDays(7);
            var authToken = parameters?.AuthToken;

            if (string.IsNullOrEmpty(authToken))
            {
                return TaskExecutionResult.CreateFailure("Authentication token is required", 
                    "Missing authToken in task parameters");
            }

            _logger.LogInformation("Processing Buntzen Lake reservation for {Date} using token {Token}", 
                reservationDate.ToString("yyyy-MM-dd"), RedactToken(authToken));

            // Make reservation using YodelPass API calls
            var reservationResult = await ProcessBuntzenReservation(reservationDate, authToken, context.CancellationToken);

            var resultData = new Dictionary<string, object>
            {
                ["status"] = reservationResult.Status,
                ["processedAt"] = DateTime.UtcNow
            };

            var message = reservationResult.Status == "Confirmed" 
                ? $"Buntzen Lake reservation confirmed for {reservationDate:yyyy-MM-dd}"
                : $"Buntzen Lake reservation failed: {reservationResult.Status}";

            return reservationResult.Status == "Confirmed" 
                ? TaskExecutionResult.CreateSuccess(message, resultData)
                : TaskExecutionResult.CreateFailure(message, reservationResult.Status);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Buntzen reservation task {TaskId} was cancelled", context.TaskId);
            return TaskExecutionResult.CreateFailure("Buntzen reservation task was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Buntzen reservation task {TaskId}", context.TaskId);
            return TaskExecutionResult.CreateFailure("Buntzen reservation failed", ex.Message);
        }
        finally
        {
            result.ExecutionEndTime = DateTime.UtcNow;
        }
    }

    private async Task<BuntzenReservationResult> ProcessBuntzenReservation(
        DateTime reservationDate, 
        string authToken,
        CancellationToken cancellationToken)
    {
        try
        {
            // YodelPass API endpoints (from the bash script)
            const string cartUrl = "https://api.yodelpass.com/api/cart?IsWeb=true";
            const string checkoutUrl = "https://api.yodelpass.com/api/orders/checkout";
            
            // Prepare reservation data following the exact bash script format
            var reservationData = new
            {
                catalogItemId = 11584, 
                placeId = 10672,
                // catalogItemId = 12196, 
                // placeId = 10671,
                quantity = 1,
                effectiveDateLtc = reservationDate.ToString("yyyy-MM-dd"),
                vehicleMakeModel = "Lamborghini Gallardo",
                vehicleLicensePlate = "CHGTHS",
                vehicleState = "BC",
                sourceScopeId = 14
            };
            
            // Step 1: Add item to cart with retry logic
            var cartSuccess = await AddItemToCart(cartUrl, reservationData, authToken, cancellationToken);
            if (!cartSuccess.Success)
            {
                return new BuntzenReservationResult { Status = cartSuccess.ErrorMessage };
            }
            
            // Step 2: Checkout
            return await ProcessCheckout(checkoutUrl, authToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Buntzen reservation process");
            return new BuntzenReservationResult
            {
                Status = ex switch
                {
                    HttpRequestException => "Network Error - Unable to connect to YodelPass API",
                    TaskCanceledException when ex.InnerException is TimeoutException => "Timeout - YodelPass API did not respond in time",
                    TaskCanceledException => "Request was cancelled",
                    JsonException => "API Error - Invalid response format from YodelPass",
                    _ => $"Unexpected error: {ex.Message}"
                }
            };
        }
    }

    private async Task<(bool Success, string ErrorMessage)> AddItemToCart(
        string cartUrl, 
        object reservationData, 
        string authToken, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding Buntzen Lake reservation to cart...");
        
        const int maxAttempts = 100;
        const int waitTimeSeconds = 5;
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            _logger.LogInformation("Adding item to cart, attempt {Attempt}", attempt);
            
            var cartRequest = new HttpRequestMessage(HttpMethod.Put, cartUrl);
            cartRequest.Headers.Add("x-api-version", "5.6");
            cartRequest.Headers.Add("Authorization", $"Bearer {authToken}");
            
            var requestJson = JsonSerializer.Serialize(reservationData);
            cartRequest.Content = new StringContent(
                requestJson,
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            // Log the request
            _logger.LogInformation("Cart API Request - Attempt {Attempt}:\nURL: {Url}\nMethod: {Method}\nHeaders: x-api-version=5.6, Authorization=Bearer {Token}\nBody: {RequestBody}", 
                attempt, cartUrl, "PUT", RedactToken(authToken), requestJson);
            
            var cartResponse = await _httpClient.SendAsync(cartRequest, cancellationToken);
            
            // Log the response
            var responseContent = await cartResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Cart API Response - Attempt {Attempt}:\nStatus: {StatusCode} {StatusText}\nBody: {ResponseBody}", 
                attempt, (int)cartResponse.StatusCode, cartResponse.ReasonPhrase, responseContent);
            
            if (cartResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Item added to cart successfully on attempt {Attempt}", attempt);
                return (true, string.Empty);
            }
            
            if (cartResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Authentication failed - invalid token");
                return (false, "Authentication Failed - Invalid or expired token");
            }
            
            _logger.LogWarning("Attempt {Attempt} failed with status {Status}, trying again in {Wait} seconds. Error: {Error}", 
                attempt, (int)cartResponse.StatusCode, waitTimeSeconds, responseContent);
            
            if (attempt < maxAttempts)
            {
                await Task.Delay(waitTimeSeconds * 1000, cancellationToken);
            }
        }
        
        return (false, "Failed - Unable to add reservation to cart after multiple attempts");
    }
    
    private async Task<BuntzenReservationResult> ProcessCheckout(
        string checkoutUrl, 
        string authToken, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking out...");
        
        var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, checkoutUrl);
        checkoutRequest.Headers.Add("x-api-version", "5.6");
        checkoutRequest.Headers.Add("Authorization", $"Bearer {authToken}");
        
        var requestBody = "{}";
        checkoutRequest.Content = new StringContent(
            requestBody,
            System.Text.Encoding.UTF8,
            "application/json"
        );
        
        // Log the request
        _logger.LogInformation("Checkout API Request:\nURL: {Url}\nMethod: {Method}\nHeaders: x-api-version=5.6, Authorization=Bearer {Token}\nBody: {RequestBody}", 
            checkoutUrl, "POST", RedactToken(authToken), requestBody);
        
        var checkoutResponse = await _httpClient.SendAsync(checkoutRequest, cancellationToken);
        
        // Log the response
        var checkoutContent = await checkoutResponse.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("Checkout API Response:\nStatus: {StatusCode} {StatusText}\nBody: {ResponseBody}", 
            (int)checkoutResponse.StatusCode, checkoutResponse.ReasonPhrase, checkoutContent);
        
        if (checkoutResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully checked out. Response: {Response}", checkoutContent);
            
            return new BuntzenReservationResult { Status = "Confirmed" };
        }
        
        _logger.LogWarning("Failed to checkout with status {Status}. Error: {Error}", 
            (int)checkoutResponse.StatusCode, checkoutContent);
        
        var errorMessage = checkoutResponse.StatusCode switch
        {
            System.Net.HttpStatusCode.PaymentRequired => "Payment required - insufficient funds in YodelPass wallet",
            System.Net.HttpStatusCode.Conflict => "Reservation no longer available",
            System.Net.HttpStatusCode.BadRequest => "Invalid checkout request",
            _ => $"Checkout failed (HTTP {(int)checkoutResponse.StatusCode})"
        };
        
        return new BuntzenReservationResult { Status = $"Failed - {errorMessage}" };
    }
    
    private string RedactToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length <= 8)
            return "[REDACTED]";
        
        return $"{token[..4]}...{token[^4..]}";
    }
    
    private BuntzenReservationParameters? ParseParameters(string? parametersJson)
    {
        if (string.IsNullOrEmpty(parametersJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BuntzenReservationParameters>(parametersJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Buntzen reservation parameters: {Parameters}", parametersJson);
            return null;
        }
    }

    private class BuntzenReservationParameters
    {
        public DateTime Date { get; set; }
        public string? AuthToken { get; set; }
    }

    private class BuntzenReservationResult
    {
        public string Status { get; set; } = string.Empty;
    }
}
