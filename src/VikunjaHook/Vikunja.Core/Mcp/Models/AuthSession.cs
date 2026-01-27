namespace Vikunja.Core.Mcp.Models;

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
)
{
    /// <summary>
    /// Alias for CreatedAt (for backward compatibility)
    /// </summary>
    public DateTime Created => CreatedAt;

    /// <summary>
    /// Last accessed time (defaults to creation time)
    /// </summary>
    public DateTime LastAccessed { get; init; } = CreatedAt;

    /// <summary>
    /// Check if session is expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
};
