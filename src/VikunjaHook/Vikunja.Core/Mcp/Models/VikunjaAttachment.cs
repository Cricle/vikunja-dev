namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a task attachment
/// </summary>
public record VikunjaAttachment(
    long Id,
    long TaskId,
    string FileName,
    long FileSize,
    string? MimeType,
    DateTime Created,
    VikunjaUser? CreatedBy
);
