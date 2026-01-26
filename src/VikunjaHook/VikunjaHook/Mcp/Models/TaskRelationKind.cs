namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Types of relationships between tasks
/// </summary>
public enum TaskRelationKind
{
    Unknown,
    Subtask,
    Parenttask,
    Related,
    Duplicateof,
    Duplicates,
    Blocking,
    Blocked,
    Precedes,
    Follows,
    Copiedfrom,
    Copiedto
}
