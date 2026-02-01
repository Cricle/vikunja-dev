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
}
