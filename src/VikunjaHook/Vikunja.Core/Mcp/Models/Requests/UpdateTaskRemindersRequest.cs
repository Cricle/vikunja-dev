using System.Text.Json.Serialization;

namespace Vikunja.Core.Mcp.Models.Requests;

/// <summary>
/// Request to update task reminders
/// </summary>
public record UpdateTaskRemindersRequest(
    [property: JsonPropertyName("reminders")] List<ReminderItem> Reminders
);
