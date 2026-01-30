namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a task relation in Vikunja
/// </summary>
public record VikunjaTaskRelation(
    long TaskId,
    long OtherTaskId,
    string RelationKind,
    DateTime Created,
    VikunjaUser? CreatedBy
);
