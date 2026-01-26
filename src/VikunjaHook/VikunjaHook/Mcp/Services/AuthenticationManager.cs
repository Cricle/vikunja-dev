using System.Collections.Concurrent;
using VikunjaHook.Mcp.Models;

namespace VikunjaHook.Mcp.Services;

/// <summary>
/// Manages authentication sessions for MCP clients
/// </summary>
public class AuthenticationManager : IAuthenticationManager
{
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthenticationManager> _logger;

    public AuthenticationManager(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthenticationManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Validate and store authentication session
    /// </summary>
    public async Task<AuthSession> AuthenticateAsync(
        string apiUrl, 
        string apiToken, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentException("API URL cannot be empty", nameof(apiUrl));
        
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("API token cannot be empty", nameof(apiToken));

        // Detect authentication type
        var authType = DetectAuthType(apiToken);
        
        _logger.LogDebug("Authenticating with {AuthType} token", authType);

        // Ensure API URL ends with /api/v1
        var normalizedApiUrl = apiUrl.TrimEnd('/');
        if (!normalizedApiUrl.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            normalizedApiUrl += "/api/v1";
        }

        // Validate token by making a test request to Vikunja API
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(normalizedApiUrl + "/"); // Ensure trailing slash for relative paths
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
            
            // Test the token by fetching user info (works for both API tokens and JWT)
            // Use relative path (no leading slash) so it appends to BaseAddress
            var response = await client.GetAsync("user", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Authentication failed with status code: {StatusCode}", response.StatusCode);
                throw new AuthenticationException(
                    $"Invalid token: API returned {response.StatusCode}",
                    new Dictionary<string, object?>
                    {
                        ["statusCode"] = (int)response.StatusCode,
                        ["authType"] = authType.ToString()
                    });
            }

            // Create session
            var sessionId = Guid.NewGuid().ToString();
            var session = new AuthSession(
                SessionId: sessionId,
                ApiUrl: normalizedApiUrl,
                ApiToken: apiToken,
                AuthType: authType,
                CreatedAt: DateTime.UtcNow
            );

            _sessions[sessionId] = session;
            
            _logger.LogInformation("Authentication successful for session {SessionId}", sessionId);
            
            return session;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during authentication");
            throw new McpException(
                McpErrorCode.NetworkError,
                "Failed to connect to Vikunja API",
                new Dictionary<string, object?>
                {
                    ["apiUrl"] = apiUrl,
                    ["error"] = ex.Message
                });
        }
    }

    /// <summary>
    /// Get current authentication session
    /// </summary>
    public AuthSession GetSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new AuthenticationException(
                "Session not found or expired",
                new Dictionary<string, object?>
                {
                    ["sessionId"] = sessionId
                });
        }

        return session;
    }

    /// <summary>
    /// Check if session is authenticated
    /// </summary>
    public bool IsAuthenticated(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        return _sessions.ContainsKey(sessionId);
    }

    /// <summary>
    /// Detect authentication type from token format
    /// </summary>
    public AuthType DetectAuthType(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        // API tokens start with "tk_"
        if (token.StartsWith("tk_", StringComparison.Ordinal))
        {
            return AuthType.ApiToken;
        }

        // JWTs start with "eyJ" (base64 encoded JSON header)
        if (token.StartsWith("eyJ", StringComparison.Ordinal))
        {
            return AuthType.Jwt;
        }

        // Default to API token for backward compatibility
        _logger.LogWarning("Unable to detect token type, defaulting to ApiToken");
        return AuthType.ApiToken;
    }

    /// <summary>
    /// Remove authentication session
    /// </summary>
    public void Disconnect(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        if (_sessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("Session {SessionId} disconnected", sessionId);
        }
    }

    /// <summary>
    /// Clear all authentication sessions (called on shutdown)
    /// </summary>
    public void DisconnectAll()
    {
        var sessionCount = _sessions.Count;
        _sessions.Clear();
        _logger.LogInformation("Cleared {SessionCount} authentication sessions", sessionCount);
    }
}
