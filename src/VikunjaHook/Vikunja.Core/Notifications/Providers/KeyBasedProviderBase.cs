using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Providers;

/// <summary>
/// 基于密钥的通知提供者基类（如 API key, device key 等）
/// </summary>
public abstract class KeyBasedProviderBase : NotificationProviderBase
{
    /// <summary>
    /// 配置中的密钥名称（如 "pushkey", "deviceKey"）
    /// </summary>
    protected abstract string KeySettingName { get; }

    /// <summary>
    /// 密钥的显示名称（用于错误消息）
    /// </summary>
    protected abstract string KeyDisplayName { get; }

    protected KeyBasedProviderBase(ILogger logger) : base(logger)
    {
    }

    /// <summary>
    /// 使用密钥发送通知
    /// </summary>
    public async Task<NotificationResult> SendAsync(
        NotificationMessage message,
        string? key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return CreateErrorResult($"{KeyDisplayName} is required");
        }

        return await SendWithRetryAsync(
            () => SendWithKeyAsync(message, key, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// 默认的 SendCoreAsync 实现，返回需要密钥的错误
    /// </summary>
    protected override Task<NotificationResult> SendCoreAsync(
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            CreateErrorResult($"{ProviderType} requires {KeyDisplayName}. Use SendAsync(message, key) instead.")
        );
    }

    /// <summary>
    /// 使用密钥发送通知的具体实现
    /// </summary>
    protected abstract Task<NotificationResult> SendWithKeyAsync(
        NotificationMessage message,
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// 默认的验证实现
    /// </summary>
    protected override async Task<ValidationResult> ValidateConfigCoreAsync(
        ProviderConfig config,
        CancellationToken cancellationToken)
    {
        return await ValidateWithTestNotificationAsync(
            config,
            KeySettingName,
            KeyDisplayName,
            SendAsync,
            cancellationToken
        );
    }
}
