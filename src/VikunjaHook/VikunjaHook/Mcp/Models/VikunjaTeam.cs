namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Represents a Vikunja team
/// </summary>
public record VikunjaTeam(
    long Id,
    string Name,
    string? Description,
    DateTime Created,
    DateTime Updated
);
