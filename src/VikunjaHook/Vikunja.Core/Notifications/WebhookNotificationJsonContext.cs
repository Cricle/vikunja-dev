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
[JsonSerializable(typeof(NotificationTemplate))]
[JsonSerializable(typeof(NotificationMessage))]
[JsonSerializable(typeof(ValidationResult))]
[JsonSerializable(typeof(TemplateContext))]
[JsonSerializable(typeof(TaskTemplateData))]
[JsonSerializable(typeof(ProjectTemplateData))]
[JsonSerializable(typeof(ScheduledPushConfig))]
[JsonSerializable(typeof(List<ScheduledPushConfig>))]
[JsonSerializable(typeof(ScheduledPushRecord))]
[JsonSerializable(typeof(List<ScheduledPushRecord>))]
[JsonSerializable(typeof(UserTemplateData))]
[JsonSerializable(typeof(TeamTemplateData))]
[JsonSerializable(typeof(LabelTemplateData))]
[JsonSerializable(typeof(CommentTemplateData))]
[JsonSerializable(typeof(AttachmentTemplateData))]
[JsonSerializable(typeof(RelationTemplateData))]
[JsonSerializable(typeof(EventData))]
[JsonSerializable(typeof(TaskEventData))]
[JsonSerializable(typeof(ProjectEventData))]
[JsonSerializable(typeof(CommentEventData))]
[JsonSerializable(typeof(AttachmentEventData))]
[JsonSerializable(typeof(RelationEventData))]
[JsonSerializable(typeof(TeamEventData))]
[JsonSerializable(typeof(List<UserConfig>))]
[JsonSerializable(typeof(List<ProviderConfig>))]
[JsonSerializable(typeof(Dictionary<string, NotificationTemplate>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class WebhookNotificationJsonContext : JsonSerializerContext
{
}
