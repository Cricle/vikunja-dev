namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a Vikunja user
/// </summary>
public record VikunjaUser(
    long Id,
    string Username,
    string? Name,
    string? Email,
    DateTime Created,
    DateTime Updated
);
