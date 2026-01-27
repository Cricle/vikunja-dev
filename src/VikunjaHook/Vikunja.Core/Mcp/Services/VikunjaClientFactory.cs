using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Models;

namespace Vikunja.Core.Mcp.Services;

/// <summary>
/// Factory for creating and managing HTTP clients for Vikunja API
/// </summary>
public class VikunjaClientFactory : IVikunjaClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VikunjaClientFactory> _logger;
    private readonly string _apiUrl;
    private readonly string _apiToken;

    public VikunjaClientFactory(
        IHttpClientFactory httpClientFactory,
        ILogger<VikunjaClientFactory> logger,
        string apiUrl,
        string apiToken)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiUrl = apiUrl.TrimEnd('/') + "/";
        _apiToken = apiToken;
    }

    /// <summary>
    /// Get or create HTTP client for Vikunja API
    /// </summary>
    public HttpClient GetClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_apiUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
        client.Timeout = TimeSpan.FromSeconds(30);
        
        return client;
    }

    /// <summary>
    /// Execute GET request to Vikunja API
    /// </summary>
    public async Task<T> GetAsync<T>(
        string endpoint, 
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        
        _logger.LogDebug("GET request to {Endpoint}", endpoint);
        
        var response = await client.GetAsync(endpoint, cancellationToken);
        
        await EnsureSuccessStatusCodeAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Deserialize using the context
        var result = JsonSerializer.Deserialize(json, typeof(T), AppJsonSerializerContext.Default);
        
        if (result is not T typedResult)
        {
            throw new McpException(
                McpErrorCode.InternalError,
                $"Failed to deserialize response to type {typeof(T).Name}");
        }
        
        return typedResult;
    }

    /// <summary>
    /// Execute POST request to Vikunja API
    /// </summary>
    public async Task<T> PostAsync<T>(
        string endpoint, 
        object body, 
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        
        _logger.LogDebug("POST request to {Endpoint}", endpoint);
        
        // Serialize body
        var bodyJson = JsonSerializer.Serialize(body, body.GetType(), AppJsonSerializerContext.Default);
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(endpoint, content, cancellationToken);
        
        await EnsureSuccessStatusCodeAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Deserialize using the context
        var result = JsonSerializer.Deserialize(json, typeof(T), AppJsonSerializerContext.Default);
        
        if (result is not T typedResult)
        {
            throw new McpException(
                McpErrorCode.InternalError,
                $"Failed to deserialize response to type {typeof(T).Name}");
        }
        
        return typedResult;
    }

    /// <summary>
    /// Execute PUT request to Vikunja API
    /// </summary>
    public async Task<T> PutAsync<T>(
        string endpoint, 
        object body, 
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        
        _logger.LogDebug("PUT request to {Endpoint}", endpoint);
        
        // Serialize body
        var bodyJson = JsonSerializer.Serialize(body, body.GetType(), AppJsonSerializerContext.Default);
        var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        
        var response = await client.PutAsync(endpoint, content, cancellationToken);
        
        await EnsureSuccessStatusCodeAsync(response);
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Deserialize using the context
        var result = JsonSerializer.Deserialize(json, typeof(T), AppJsonSerializerContext.Default);
        
        if (result is not T typedResult)
        {
            throw new McpException(
                McpErrorCode.InternalError,
                $"Failed to deserialize response to type {typeof(T).Name}");
        }
        
        return typedResult;
    }

    /// <summary>
    /// Execute DELETE request to Vikunja API
    /// </summary>
    public async Task DeleteAsync(
        string endpoint, 
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        
        _logger.LogDebug("DELETE request to {Endpoint}", endpoint);
        
        var response = await client.DeleteAsync(endpoint, cancellationToken);
        
        await EnsureSuccessStatusCodeAsync(response);
    }

    /// <summary>
    /// Ensure HTTP response is successful, throw appropriate exception if not
    /// </summary>
    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogWarning(
            "HTTP request failed with status {StatusCode}: {Content}", 
            response.StatusCode, 
            content);

        var errorCode = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => McpErrorCode.InvalidToken,
            HttpStatusCode.Forbidden => McpErrorCode.InsufficientPermissions,
            HttpStatusCode.NotFound => McpErrorCode.ResourceNotFound,
            HttpStatusCode.TooManyRequests => McpErrorCode.RateLimitExceeded,
            HttpStatusCode.ServiceUnavailable => McpErrorCode.ServiceUnavailable,
            _ => McpErrorCode.OperationFailed
        };

        throw new McpException(
            errorCode,
            $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
            new Dictionary<string, object?>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["content"] = content
            });
    }
}
