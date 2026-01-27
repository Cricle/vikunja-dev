using System.Text.Json.Serialization;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(JsonStringEnumConverter<NotificationFormat>)])]
[JsonSerializable(typeof(UserConfig))]
[JsonSerializable(typeof(WebhookEvent))]
[JsonSerializable(typeof(NotificationResult))]
[JsonSerializable(typeof(ProviderConfig))]
[JsonSerializable(typeof(ProjectRule))]
[JsonSerializable(typeof(NotificationTemplate))]
[JsonSerializable(typeof(NotificationMessage))]
[JsonSerializable(typeof(ValidationResult))]
[JsonSerializable(typeof(TemplateContext))]
[JsonSerializable(typeof(TaskData))]
[JsonSerializable(typeof(ProjectData))]
[JsonSerializable(typeof(UserData))]
[JsonSerializable(typeof(EventData))]
[JsonSerializable(typeof(TaskEventData))]
[JsonSerializable(typeof(ProjectEventData))]
[JsonSerializable(typeof(CommentEventData))]
[JsonSerializable(typeof(AttachmentEventData))]
[JsonSerializable(typeof(RelationEventData))]
[JsonSerializable(typeof(TeamEventData))]
[JsonSerializable(typeof(List<UserConfig>))]
[JsonSerializable(typeof(List<ProviderConfig>))]
[JsonSerializable(typeof(List<ProjectRule>))]
[JsonSerializable(typeof(Dictionary<string, NotificationTemplate>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class WebhookNotificationJsonContext : JsonSerializerContext
{
}
