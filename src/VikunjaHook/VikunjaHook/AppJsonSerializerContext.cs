using System.Text.Json.Serialization;
using VikunjaHook.Models;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Models.Requests;
using VikunjaHook.Mcp.Models.Configuration;
using VikunjaHook.Mcp.Controllers;

namespace VikunjaHook;

// Webhook models
[JsonSerializable(typeof(VikunjaWebhookPayload))]
[JsonSerializable(typeof(WebhookData))]
[JsonSerializable(typeof(TaskData))]
[JsonSerializable(typeof(ProjectData))]
[JsonSerializable(typeof(UserData))]
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
[JsonSerializable(typeof(CreateLabelRequest))]
[JsonSerializable(typeof(AddCommentRequest))]
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
[JsonSerializable(typeof(ConfigurationResponse))]
[JsonSerializable(typeof(ConfigurationUpdateRequest))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ErrorDetail))]

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
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Dictionary<string, object?>>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]

// ASP.NET Core types
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
