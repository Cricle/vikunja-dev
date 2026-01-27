namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Represents a Vikunja webhook
/// </summary>
public record VikunjaWebhook(
    long Id,
    long ProjectId,
    string TargetUrl,
    List<string> Events,
    string? Secret,
    DateTime Created,
    DateTime Updated
);
