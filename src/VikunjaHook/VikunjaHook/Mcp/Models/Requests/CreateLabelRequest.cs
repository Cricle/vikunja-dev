namespace VikunjaHook.Mcp.Models.Requests;

/// <summary>
/// Request to create a new label
/// </summary>
public record CreateLabelRequest(
    string Title,
    string? Description = null,
    string? HexColor = null
);
