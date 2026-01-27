namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a comment on a Vikunja task
/// </summary>
public record VikunjaComment(
    long Id,
    string Comment,
    VikunjaUser? Author,
    DateTime Created,
    DateTime Updated
);
