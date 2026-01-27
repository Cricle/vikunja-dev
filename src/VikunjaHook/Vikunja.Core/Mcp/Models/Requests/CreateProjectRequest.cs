namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request model for creating a new project
/// </summary>
public record CreateProjectRequest(
    string Title,
    string? Description = null,
    string? HexColor = null,
    long? ParentProjectId = null
);
