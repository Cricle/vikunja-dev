namespace Vikunja.Core.Mcp.Services;

/// <summary>
/// Factory for creating and managing HTTP clients for Vikunja API
/// </summary>
public interface IVikunjaClientFactory
{
    /// <summary>
    /// Get or create HTTP client for Vikunja API
    /// </summary>
    HttpClient GetClient();
    
    /// <summary>
    /// Execute GET request to Vikunja API
    /// </summary>
    Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute POST request to Vikunja API
    /// </summary>
    Task<T> PostAsync<T>(string endpoint, object body, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute PUT request to Vikunja API
    /// </summary>
    Task<T> PutAsync<T>(string endpoint, object body, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute DELETE request to Vikunja API
    /// </summary>
    Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}
