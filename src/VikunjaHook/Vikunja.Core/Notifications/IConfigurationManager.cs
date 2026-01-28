using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public interface IConfigurationManager
{
    Task<UserConfig?> LoadUserConfigAsync(
        string userId, 
        CancellationToken cancellationToken = default);
    
    Task SaveUserConfigAsync(
        UserConfig config, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<UserConfig>> LoadAllConfigsAsync(
        CancellationToken cancellationToken = default);
    
    Task<byte[]> ExportConfigsAsync(
        IEnumerable<string> userIds, 
        CancellationToken cancellationToken = default);
    
    Task ImportConfigsAsync(
        byte[] zipData, 
        CancellationToken cancellationToken = default);
}
