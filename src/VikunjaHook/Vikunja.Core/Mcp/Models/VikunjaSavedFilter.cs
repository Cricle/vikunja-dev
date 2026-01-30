namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a saved filter in Vikunja
/// </summary>
public record VikunjaSavedFilter(
    long Id,
    string Title,
    string? Description,
    string Filters,
    DateTime Created,
    DateTime Updated
);
