using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Interfaces;

public interface INotificationProvider
{
    string ProviderType { get; }
    
    Task<NotificationResult> SendAsync(
        NotificationMessage message, 
        CancellationToken cancellationToken = default);
    
    Task<ValidationResult> ValidateConfigAsync(
        ProviderConfig config, 
        CancellationToken cancellationToken = default);
}
