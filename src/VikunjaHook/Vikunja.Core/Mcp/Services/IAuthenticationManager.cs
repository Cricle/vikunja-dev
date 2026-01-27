using Vikunja.Core.Mcp.Models;

namespace Vikunja.Core.Mcp.Services;

/// <summary>
/// Manages authentication sessions for MCP clients
/// </summary>
public interface IAuthenticationManager
{
    /// <summary>
    /// Validate and store authentication session
    /// </summary>
    Task<AuthSession> AuthenticateAsync(string apiUrl, string apiToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current authentication session
    /// </summary>
    AuthSession GetSession(string sessionId);
    
    /// <summary>
    /// Check if session is authenticated
    /// </summary>
    bool IsAuthenticated(string sessionId);
    
    /// <summary>
    /// Detect authentication type from token format
    /// </summary>
    AuthType DetectAuthType(string token);
    
    /// <summary>
    /// Remove authentication session
    /// </summary>
    bool Disconnect(string sessionId);
    
    /// <summary>
    /// Get all active sessions
    /// </summary>
    IReadOnlyList<AuthSession> GetAllSessions();
    
    /// <summary>
    /// Clear all authentication sessions (called on shutdown)
    /// </summary>
    void DisconnectAll();
}
