using System.Text.Json.Serialization;

namespace Vikunja.Core.Notifications.Models;

/// <summary>
/// 定时推送配置
/// </summary>
public sealed class ScheduledPushConfig
{
    /// <summary>
    /// 配置 ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 用户 ID
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 推送时间（24小时制，如 "09:00"）
    /// </summary>
    [JsonPropertyName("pushTime")]
    public string PushTime { get; set; } = "09:00";

    /// <summary>
    /// 最低优先级（0-5，包含此优先级及以上的任务）
    /// </summary>
    [JsonPropertyName("minPriority")]
    public int MinPriority { get; set; } = 0;

    /// <summary>
    /// 标签 ID 列表（OR 运算）
    /// </summary>
    [JsonPropertyName("labelIds")]
    public List<long> LabelIds { get; set; } = new();

    /// <summary>
    /// 推送标题模板
    /// </summary>
    [JsonPropertyName("titleTemplate")]
    public string TitleTemplate { get; set; } = "📋 今日待办任务";

    /// <summary>
    /// 推送正文模板（Markdown 格式）
    /// </summary>
    [JsonPropertyName("bodyTemplate")]
    public string BodyTemplate { get; set; } = "## 未完成的任务\n\n{{tasks}}\n\n共 {{count}} 个任务待处理";

    /// <summary>
    /// 推送提供商列表
    /// </summary>
    [JsonPropertyName("providers")]
    public List<string> Providers { get; set; } = new();

    /// <summary>
    /// 最后推送时间
    /// </summary>
    [JsonPropertyName("lastPushTime")]
    public DateTime? LastPushTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 定时推送记录
/// </summary>
public sealed class ScheduledPushRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("configId")]
    public string ConfigId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("taskCount")]
    public int TaskCount { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("providers")]
    public List<string> Providers { get; set; } = new();

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
