namespace FlightClub.Services.Security;

/// <summary>
/// Service for obfuscating sensitive data in task parameters
/// </summary>
public interface IParameterObfuscationService
{
    /// <summary>
    /// Obfuscates sensitive data in task parameters based on task type
    /// </summary>
    /// <param name="parametersJson">The parameters JSON string</param>
    /// <param name="taskType">The type of task</param>
    /// <returns>Parameters JSON with obfuscated sensitive data</returns>
    string? ObfuscateParameters(string? parametersJson, string taskType);
}
