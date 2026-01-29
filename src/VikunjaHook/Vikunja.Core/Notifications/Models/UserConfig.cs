namespace Vikunja.Core.Notifications.Models;

public class UserConfig
{
    public string UserId { get; set; } = string.Empty;
    public List<ProviderConfig> Providers { get; set; } = new();
    public List<string> DefaultProviders { get; set; } = new(); // Default provider types to use
    public Dictionary<string, NotificationTemplate> Templates { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Task reminder configuration
    /// </summary>
    public TaskReminderConfig? ReminderConfig { get; set; }
}

public class TaskReminderConfig
{
    /// <summary>
    /// Enable task reminder scanning
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Scan interval in seconds (default: 10)
    /// </summary>
    public int ScanIntervalSeconds { get; set; } = 10;
    
    /// <summary>
    /// Notification format
    /// </summary>
    public NotificationFormat Format { get; set; } = NotificationFormat.Text;
    
    /// <summary>
    /// Specific providers for reminders (empty = use default)
    /// </summary>
    public List<string> Providers { get; set; } = new();
    
    /// <summary>
    /// Enable label filtering (default: false)
    /// </summary>
    public bool EnableLabelFilter { get; set; } = false;
    
    /// <summary>
    /// Label IDs to filter (OR logic - task must have at least one of these labels)
    /// </summary>
    public List<long> FilterLabelIds { get; set; } = new();
    
    /// <summary>
    /// Template for start date reminders
    /// </summary>
    public ReminderTemplate StartDateTemplate { get; set; } = new()
    {
        TitleTemplate = "🚀 Task Starting: {{task.title}}",
        BodyTemplate = "Task: {{task.title}}\nProject: {{project.title}}\nStart Time: {{task.startDate}}"
    };
    
    /// <summary>
    /// Template for due date reminders
    /// </summary>
    public ReminderTemplate DueDateTemplate { get; set; } = new()
    {
        TitleTemplate = "⏰ Task Due Soon: {{task.title}}",
        BodyTemplate = "Task: {{task.title}}\nProject: {{project.title}}\nDue Time: {{task.dueDate}}"
    };
    
    /// <summary>
    /// Template for end date reminders
    /// </summary>
    public ReminderTemplate EndDateTemplate { get; set; } = new()
    {
        TitleTemplate = "🏁 Task Ending: {{task.title}}",
        BodyTemplate = "Task: {{task.title}}\nProject: {{project.title}}\nEnd Time: {{task.endDate}}"
    };
    
    /// <summary>
    /// Template for reminder time notifications
    /// </summary>
    public ReminderTemplate ReminderTimeTemplate { get; set; } = new()
    {
        TitleTemplate = "🔔 Task Reminder: {{task.title}}",
        BodyTemplate = "Task: {{task.title}}\nProject: {{project.title}}\nReminder: {{task.reminders}}"
    };
}

public class ReminderTemplate
{
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}

public class NotificationTemplate
{
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationFormat Format { get; set; } = NotificationFormat.Text;
    public List<string> Providers { get; set; } = new(); // Specific providers for this template (empty = use default)
    
    /// <summary>
    /// For task.updated events: if true, only notify when task is marked as done. Default is false (notify on all updates).
    /// </summary>
    public bool OnlyNotifyWhenCompleted { get; set; } = false;
}
