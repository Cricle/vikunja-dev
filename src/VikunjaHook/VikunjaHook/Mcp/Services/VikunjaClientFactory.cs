using System.Net;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using VikunjaHook.Mcp.Models;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// Factory for creating and managing HTTP clients for Vikunja API with retry logic
/// </summary>
public class VikunjaClientFactory : IVikunjaClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VikunjaClientFactory> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public VikunjaClientFactory(
        IHttpClientFactory httpClientFactory,
        ILogger<VikunjaClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Create resilience pipeline with retry and circuit breaker
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();
    }

    /// <summary>
    /// Get or create HTTP client for Vikunja API
    /// </summary>
    public HttpClient GetClient(AuthSession session)
    {
        var client = _httpClientFactory.CreateClient();
        // Ensure trailing slash for proper relative path resolution
        var baseUrl = session.ApiUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {session.ApiToken}");
        client.Timeout = TimeSpan.FromSeconds(30);
        
        return client;
    }

    /// <summary>
    /// Execute GET request to Vikunja API
    /// Note: Returns object to avoid AOT issues with generic deserialization
    /// Caller should cast to expected type
    /// </summary>
    public async Task<T> GetAsync<T>(
        AuthSession session, 
        string endpoint, 
        CancellationToken cancellationToken = default)
    {
        var json = await _resiliencePipeline.ExecuteAsync(async token =>
        {
            var client = GetClient(session);
            
            _logger.LogDebug("GET request to {Endpoint}", endpoint);
            
            var response = await client.GetAsync(endpoint, token);
            
            await EnsureSuccessStatusCodeAsync(response);
            
            return await response.Content.ReadAsStringAsync(token);
        }, cancellationToken);

        // Deserialize using the context - caller must ensure T is registered
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
        AuthSession session, 
        string endpoint, 
        object body, 
        CancellationToken cancellationToken = default)
    {
        var json = await _resiliencePipeline.ExecuteAsync(async token =>
        {
            var client = GetClient(session);
            
            _logger.LogDebug("POST request to {Endpoint}", endpoint);
            
            // Serialize body
            var bodyJson = JsonSerializer.Serialize(body, body.GetType(), AppJsonSerializerContext.Default);
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(endpoint, content, token);
            
            await EnsureSuccessStatusCodeAsync(response);
            
            return await response.Content.ReadAsStringAsync(token);
        }, cancellationToken);

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
        AuthSession session, 
        string endpoint, 
        object body, 
        CancellationToken cancellationToken = default)
    {
        var json = await _resiliencePipeline.ExecuteAsync(async token =>
        {
            var client = GetClient(session);
            
            _logger.LogDebug("PUT request to {Endpoint}", endpoint);
            
            // Serialize body
            var bodyJson = JsonSerializer.Serialize(body, body.GetType(), AppJsonSerializerContext.Default);
            var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            
            var response = await client.PutAsync(endpoint, content, token);
            
            await EnsureSuccessStatusCodeAsync(response);
            
            return await response.Content.ReadAsStringAsync(token);
        }, cancellationToken);

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
        AuthSession session, 
        string endpoint, 
        CancellationToken cancellationToken = default)
    {
        await _resiliencePipeline.ExecuteAsync(async token =>
        {
            var client = GetClient(session);
            
            _logger.LogDebug("DELETE request to {Endpoint}", endpoint);
            
            var response = await client.DeleteAsync(endpoint, token);
            
            await EnsureSuccessStatusCodeAsync(response);
        }, cancellationToken);
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
