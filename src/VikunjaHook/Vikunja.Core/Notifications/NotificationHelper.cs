using Microsoft.Extensions.Logging;
using Vikunja.Core.Notifications.Models;
using Vikunja.Core.Notifications.Providers;

namespace Vikunja.Core.Notifications;

/// <summary>
/// 通知发送辅助类，统一处理不同 provider 的发送逻辑
/// </summary>
public static class NotificationHelper
{
    /// <summary>
    /// 发送通知，自动处理不同 provider 的特殊逻辑
    /// </summary>
    public static async Task<NotificationResult> SendNotificationAsync(
        NotificationProviderBase provider,
        ProviderConfig providerConfig,
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        // PushDeer 需要 pushkey
        if (provider is PushDeerProvider pushDeer &&
            providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
        {
            return await pushDeer.SendAsync(message, pushKey, cancellationToken);
        }
        
        // Bark 需要 deviceKey
        if (provider is BarkProvider bark &&
            providerConfig.Settings.TryGetValue("deviceKey", out var deviceKey))
        {
            return await bark.SendAsync(message, deviceKey, cancellationToken);
        }
        
        // 其他 provider 使用默认方法
        return await provider.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// 批量发送通知到多个 providers
    /// </summary>
    public static async Task<bool> SendToProvidersAsync(
        IEnumerable<NotificationProviderBase> allProviders,
        IEnumerable<string> providerTypes,
        IEnumerable<ProviderConfig> providerConfigs,
        NotificationMessage message,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var success = false;

        foreach (var providerType in providerTypes)
        {
            var provider = allProviders.FirstOrDefault(p => p.ProviderType == providerType);
            if (provider == null)
            {
                logger.LogWarning("Provider {ProviderType} not found", providerType);
                continue;
            }

            var providerConfig = providerConfigs.FirstOrDefault(p => p.ProviderType == providerType);
            if (providerConfig == null)
            {
                logger.LogWarning("Provider config for {ProviderType} not found", providerType);
                continue;
            }

            try
            {
                var result = await SendNotificationAsync(
                    provider,
                    providerConfig,
                    message,
                    cancellationToken
                );

                if (result.Success)
                {
                    success = true;
                    logger.LogInformation("✓ 推送成功 - 提供商: {Provider}", providerType);
                }
                else
                {
                    logger.LogWarning("✗ 推送失败 - 提供商: {Provider}, 错误: {Error}",
                        providerType, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "✗ 推送异常 - 提供商: {Provider}", providerType);
            }
        }

        return success;
    }
}
