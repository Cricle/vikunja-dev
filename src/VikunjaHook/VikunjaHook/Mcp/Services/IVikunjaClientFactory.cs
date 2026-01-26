using VikunjaHook.Mcp.Models;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// Factory for creating and managing HTTP clients for Vikunja API
/// </summary>
public interface IVikunjaClientFactory
{
    /// <summary>
    /// Get or create HTTP client for Vikunja API
    /// </summary>
    HttpClient GetClient(AuthSession session);
    
    /// <summary>
    /// Execute GET request to Vikunja API
    /// </summary>
    Task<T> GetAsync<T>(AuthSession session, string endpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute POST request to Vikunja API
    /// </summary>
    Task<T> PostAsync<T>(AuthSession session, string endpoint, object body, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute PUT request to Vikunja API
    /// </summary>
    Task<T> PutAsync<T>(AuthSession session, string endpoint, object body, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute DELETE request to Vikunja API
    /// </summary>
    Task DeleteAsync(AuthSession session, string endpoint, CancellationToken cancellationToken = default);
}
