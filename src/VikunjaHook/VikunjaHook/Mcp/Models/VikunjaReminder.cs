namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Represents a reminder for a Vikunja task
/// </summary>
public record VikunjaReminder(
    DateTime Reminder,
    int RelativePeriod,
    string RelativeTo
);
