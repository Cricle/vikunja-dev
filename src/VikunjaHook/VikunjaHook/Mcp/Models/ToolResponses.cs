namespace VikunjaHook.Mcp.Models;

/// <summary>
/// Base response for tool operations
/// </summary>
public record ToolResponse(
    string Message
);

/// <summary>
/// Response for project operations
/// </summary>
public record ProjectResponse(
    VikunjaProject? Project,
    string Message
);

/// <summary>
/// Response for project list operations
/// </summary>
public record ProjectListResponse(
    List<VikunjaProject> Projects,
    string Message
);

/// <summary>
/// Response for task operations
/// </summary>
public record TaskResponse(
    VikunjaTask? Task,
    string Message
);

/// <summary>
/// Response for task list operations
/// </summary>
public record TaskListResponse(
    List<VikunjaTask> Tasks,
    string Message
);

/// <summary>
/// Response for label operations
/// </summary>
public record LabelResponse(
    VikunjaLabel? Label,
    string Message
);

/// <summary>
/// Response for label list operations
/// </summary>
public record LabelListResponse(
    List<VikunjaLabel> Labels,
    string Message
);

/// <summary>
/// Response for team operations
/// </summary>
public record TeamResponse(
    VikunjaTeam? Team,
    string Message
);

/// <summary>
/// Response for team list operations
/// </summary>
public record TeamListResponse(
    List<VikunjaTeam> Teams,
    string Message
);

/// <summary>
/// Response for user operations
/// </summary>
public record UserResponse(
    VikunjaUser? User,
    string Message
);

/// <summary>
/// Response for user list operations
/// </summary>
public record UserListResponse(
    List<VikunjaUser> Users,
    string Message
);

/// <summary>
/// Response for comment operations
/// </summary>
public record CommentResponse(
    VikunjaComment? Comment,
    string Message
);

/// <summary>
/// Response for comment list operations
/// </summary>
public record CommentListResponse(
    List<VikunjaComment> Comments,
    string Message
);

/// <summary>
/// Response for task relation operations
/// </summary>
public record TaskRelationResponse(
    VikunjaTaskRelation? Relation,
    string Message
);

/// <summary>
/// Response for task relation list operations
/// </summary>
public record TaskRelationListResponse(
    List<VikunjaTaskRelation> Relations,
    string Message
);

/// <summary>
/// Response for reminder list operations
/// </summary>
public record ReminderListResponse(
    List<VikunjaReminder> Reminders,
    string Message
);

/// <summary>
/// Response for bulk operations
/// </summary>
public record BulkOperationResponse(
    int Count,
    string Message
);
