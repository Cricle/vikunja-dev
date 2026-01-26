namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Represents an authenticated session
/// </summary>
public record AuthSession(
    string SessionId,
    string ApiUrl,
    string ApiToken,
    AuthType AuthType,
    DateTime CreatedAt,
    DateTime? ExpiresAt = null,
    string? UserId = null
);
