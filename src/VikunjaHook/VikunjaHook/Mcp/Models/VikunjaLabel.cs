namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Represents a Vikunja label
/// </summary>
public record VikunjaLabel(
    long Id,
    string Title,
    string? Description,
    string? HexColor,
    DateTime Created,
    DateTime Updated
);
