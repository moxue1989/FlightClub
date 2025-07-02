using System.Text.Json;

namespace FlightClub.Services.Security;

/// <summary>
/// Service for obfuscating sensitive data in task parameters
/// </summary>
public class ParameterObfuscationService : IParameterObfuscationService
{
    /// <summary>
    /// Obfuscates sensitive data in task parameters based on task type
    /// </summary>
    /// <param name="parametersJson">The parameters JSON string</param>
    /// <param name="taskType">The type of task</param>
    /// <returns>Parameters JSON with obfuscated sensitive data</returns>
    public string? ObfuscateParameters(string? parametersJson, string taskType)
    {
        if (string.IsNullOrEmpty(parametersJson))
            return parametersJson;

        // Only obfuscate for task types that contain sensitive data
        return taskType.ToLower() switch
        {
            "reservebuntzen" => ObfuscateReserveBuntzenParameters(parametersJson),
            _ => parametersJson // No obfuscation for other task types
        };
    }

    /// <summary>
    /// Obfuscates auth tokens in ReserveBuntzen task parameters
    /// </summary>
    /// <param name="parametersJson">The parameters JSON string</param>
    /// <returns>Parameters JSON with obfuscated auth token</returns>
    private static string ObfuscateReserveBuntzenParameters(string parametersJson)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(parametersJson);
            var root = jsonDoc.RootElement;
            
            if (root.TryGetProperty("AuthToken", out var authTokenElement))
            {
                var originalToken = authTokenElement.GetString();
                if (!string.IsNullOrEmpty(originalToken))
                {
                    var obfuscatedToken = ObfuscateToken(originalToken);
                    return parametersJson.Replace($"\"{originalToken}\"", $"\"{obfuscatedToken}\"");
                }
            }
            
            return parametersJson;
        }
        catch (JsonException)
        {
            // If parsing fails, return original to avoid breaking the API
            return parametersJson;
        }
    }

    /// <summary>
    /// Obfuscates a token by showing only first and last 4 characters
    /// </summary>
    private static string ObfuscateToken(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length <= 8)
            return "[REDACTED]";
        
        return $"{token[..4]}...{token[^4..]}";
    }
}
