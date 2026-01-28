using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public interface IMcpToolsAdapter
{
    Task<ProjectData?> GetProjectAsync(
        int projectId, 
        CancellationToken cancellationToken = default);
    
    Task<TaskData?> GetTaskAsync(
        int taskId, 
        CancellationToken cancellationToken = default);
    
    Task<UserData?> GetUserAsync(
        int userId, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<string>> GetTaskAssigneesAsync(
        int taskId, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<string>> GetTaskLabelsAsync(
        int taskId, 
        CancellationToken cancellationToken = default);
}
