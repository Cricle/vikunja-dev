using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Reminder item for task update
/// </summary>
public record ReminderItem(
    [property: JsonPropertyName("reminder")] DateTime Reminder
);
