using System.Text.Json.Serialization;
using Vikunja.Core.Models;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Models.Requests;
using Vikunja.Core.Mcp.Models.Configuration;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core;

// Webhook models
[JsonSerializable(typeof(VikunjaWebhookPayload))]
[JsonSerializable(typeof(WebhookData))]
[JsonSerializable(typeof(Vikunja.Core.Models.TaskData))]
[JsonSerializable(typeof(Vikunja.Core.Models.ProjectData))]
[JsonSerializable(typeof(Vikunja.Core.Models.UserData))]
[JsonSerializable(typeof(CommentData))]
[JsonSerializable(typeof(LabelData))]
[JsonSerializable(typeof(RelationData))]
[JsonSerializable(typeof(AttachmentData))]
[JsonSerializable(typeof(TeamData))]

// MCP request/response models
[JsonSerializable(typeof(McpRequest))]
[JsonSerializable(typeof(McpResponse))]
[JsonSerializable(typeof(McpError))]
[JsonSerializable(typeof(McpServerInfo))]

// MCP authentication models
[JsonSerializable(typeof(AuthSession))]
[JsonSerializable(typeof(AuthType))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(ErrorMessage))]
[JsonSerializable(typeof(SuccessResponse))]
[JsonSerializable(typeof(WebhookSuccessResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(SupportedEventsResponse))]
[JsonSerializable(typeof(ToolExecutionResponse))]
[JsonSerializable(typeof(ToolInfo))]
[JsonSerializable(typeof(ToolsListResponse))]
[JsonSerializable(typeof(McpHealthResponse))]

// Tool response models
[JsonSerializable(typeof(ToolResponse))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(ProjectListResponse))]
[JsonSerializable(typeof(TaskResponse))]
[JsonSerializable(typeof(TaskListResponse))]
[JsonSerializable(typeof(LabelResponse))]
[JsonSerializable(typeof(LabelListResponse))]
[JsonSerializable(typeof(TeamResponse))]
[JsonSerializable(typeof(TeamListResponse))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(UserListResponse))]
[JsonSerializable(typeof(CommentResponse))]
[JsonSerializable(typeof(CommentListResponse))]
[JsonSerializable(typeof(TaskRelationResponse))]
[JsonSerializable(typeof(TaskRelationListResponse))]
[JsonSerializable(typeof(ReminderListResponse))]
[JsonSerializable(typeof(BulkOperationResponse))]

// Admin response models
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(SessionsResponse))]
[JsonSerializable(typeof(ServerStatsResponse))]
[JsonSerializable(typeof(ServerInfoStats))]
[JsonSerializable(typeof(SessionStats))]
[JsonSerializable(typeof(ToolStats))]
[JsonSerializable(typeof(MemoryStats))]
[JsonSerializable(typeof(AdminToolExecutionResponse))]
[JsonSerializable(typeof(LogEntryInfo))]
[JsonSerializable(typeof(LogsResponse))]
[JsonSerializable(typeof(MessageResponse))]

// MCP Vikunja entity models
[JsonSerializable(typeof(VikunjaTask))]
[JsonSerializable(typeof(VikunjaProject))]
[JsonSerializable(typeof(VikunjaLabel))]
[JsonSerializable(typeof(VikunjaUser))]
[JsonSerializable(typeof(VikunjaTeam))]
[JsonSerializable(typeof(VikunjaWebhook))]
[JsonSerializable(typeof(VikunjaComment))]
[JsonSerializable(typeof(VikunjaTaskRelation))]
[JsonSerializable(typeof(VikunjaReminder))]
[JsonSerializable(typeof(TaskRelationKind))]

// MCP request models
[JsonSerializable(typeof(CreateTaskRequest))]
[JsonSerializable(typeof(UpdateTaskRequest))]
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(UpdateProjectRequest))]
[JsonSerializable(typeof(CreateLabelRequest))]
[JsonSerializable(typeof(UpdateLabelRequest))]
[JsonSerializable(typeof(AddCommentRequest))]
[JsonSerializable(typeof(CreateTaskCommentRequest))]
[JsonSerializable(typeof(UpdateTaskCommentRequest))]
[JsonSerializable(typeof(AddTaskLabelRequest))]
[JsonSerializable(typeof(AddTaskAssigneeRequest))]
[JsonSerializable(typeof(TaskRelationRequest))]
[JsonSerializable(typeof(ReminderItem))]
[JsonSerializable(typeof(UpdateTaskRemindersRequest))]
[JsonSerializable(typeof(BulkAssignRequest))]
[JsonSerializable(typeof(BulkUpdateTaskDoneRequest))]
[JsonSerializable(typeof(BulkUpdateTaskPriorityRequest))]
[JsonSerializable(typeof(BulkUpdateTaskDueDateRequest))]
[JsonSerializable(typeof(BulkUpdateTaskProjectRequest))]
[JsonSerializable(typeof(ApplyLabelRequest))]

// Configuration models
[JsonSerializable(typeof(VikunjaConfiguration))]
[JsonSerializable(typeof(McpConfiguration))]
[JsonSerializable(typeof(CorsConfiguration))]
[JsonSerializable(typeof(RateLimitConfiguration))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ErrorDetail))]

// Notification models
[JsonSerializable(typeof(UserConfig))]
[JsonSerializable(typeof(ProviderConfig))]
[JsonSerializable(typeof(NotificationTemplate))]
[JsonSerializable(typeof(NotificationMessage))]
[JsonSerializable(typeof(NotificationResult))]
[JsonSerializable(typeof(WebhookEvent))]
[JsonSerializable(typeof(Vikunja.Core.Notifications.Models.TestNotificationRequest))]
[JsonSerializable(typeof(PushEventRecord))]
[JsonSerializable(typeof(ProviderPushResult))]
[JsonSerializable(typeof(List<PushEventRecord>))]
[JsonSerializable(typeof(List<ProviderPushResult>))]

// Collections
[JsonSerializable(typeof(List<VikunjaTask>))]
[JsonSerializable(typeof(List<VikunjaProject>))]
[JsonSerializable(typeof(List<VikunjaLabel>))]
[JsonSerializable(typeof(List<VikunjaUser>))]
[JsonSerializable(typeof(List<VikunjaTeam>))]
[JsonSerializable(typeof(List<VikunjaWebhook>))]
[JsonSerializable(typeof(List<VikunjaComment>))]
[JsonSerializable(typeof(List<VikunjaTaskRelation>))]
[JsonSerializable(typeof(List<VikunjaReminder>))]
[JsonSerializable(typeof(List<ProviderConfig>))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, NotificationTemplate>))]
[JsonSerializable(typeof(List<Dictionary<string, object?>>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]

// New tool types
[JsonSerializable(typeof(Vikunja.Core.Mcp.Tools.VikunjaAttachment))]
[JsonSerializable(typeof(List<Vikunja.Core.Mcp.Tools.VikunjaAttachment>))]
[JsonSerializable(typeof(Vikunja.Core.Mcp.Tools.VikunjaBucket))]
[JsonSerializable(typeof(List<Vikunja.Core.Mcp.Tools.VikunjaBucket>))]
[JsonSerializable(typeof(Vikunja.Core.Mcp.Tools.VikunjaSavedFilter))]
[JsonSerializable(typeof(List<Vikunja.Core.Mcp.Tools.VikunjaSavedFilter>))]

// Webhook response types
[JsonSerializable(typeof(Vikunja.Core.Mcp.Models.ErrorMessage))]
[JsonSerializable(typeof(Vikunja.Core.Mcp.Models.WebhookSuccessResponse))]
[JsonSerializable(typeof(Vikunja.Core.Mcp.Models.SupportedEventsResponse))]
[JsonSerializable(typeof(Vikunja.Core.Mcp.Models.HealthResponse))]

// ASP.NET Core types
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
