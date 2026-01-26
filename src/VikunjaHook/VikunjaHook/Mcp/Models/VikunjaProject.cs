namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Represents a Vikunja project
/// </summary>
public record VikunjaProject(
    long Id,
    string Title,
    string? Description,
    string? HexColor,
    bool IsArchived,
    long? ParentProjectId,
    DateTime Created,
    DateTime Updated
);
