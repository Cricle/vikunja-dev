namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a bucket (kanban column) in Vikunja
/// </summary>
public record VikunjaBucket(
    long Id,
    string Title,
    long ProjectId,
    long ProjectViewId,
    int Position,
    int Limit,
    int Count,
    DateTime Created,
    DateTime Updated
);
