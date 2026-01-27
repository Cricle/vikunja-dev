using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Tools;
using Vikunja.Core.Notifications.Interfaces;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications.Adapters;

public class McpToolsAdapter : IMcpToolsAdapter
{
    private readonly ProjectsTools _projectsTools;
    private readonly TasksTools _tasksTools;
    private readonly UsersTools _usersTools;
    private readonly ILogger<McpToolsAdapter> _logger;

    public McpToolsAdapter(
        ProjectsTools projectsTools,
        TasksTools tasksTools,
        UsersTools usersTools,
        ILogger<McpToolsAdapter> logger)
    {
        _projectsTools = projectsTools;
        _tasksTools = tasksTools;
        _usersTools = usersTools;
        _logger = logger;
    }

    public async Task<ProjectData?> GetProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await _projectsTools.GetProject(projectId, cancellationToken);
            
            return new ProjectData
            {
                Id = (int)project.Id,
                Title = project.Title,
                Description = project.Description ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<TaskData?> GetTaskAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _tasksTools.GetTask(taskId, cancellationToken);
            
            return new TaskData
            {
                Id = (int)task.Id,
                Title = task.Title,
                Description = task.Description ?? string.Empty,
                Done = task.Done
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId}", taskId);
            return null;
        }
    }

    public async Task<UserData?> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _usersTools.GetUser(userId, cancellationToken);
            
            return new UserData
            {
                Name = user.Name ?? string.Empty,
                Username = user.Username,
                Email = user.Email ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> GetTaskAssigneesAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _tasksTools.GetTask(taskId, cancellationToken);
            
            if (task.Assignees == null || task.Assignees.Count == 0)
            {
                return Array.Empty<string>();
            }

            return task.Assignees
                .Select(a => a.Name ?? a.Username)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task assignees for task {TaskId}", taskId);
            return Array.Empty<string>();
        }
    }

    public async Task<IReadOnlyList<string>> GetTaskLabelsAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _tasksTools.GetTask(taskId, cancellationToken);
            
            if (task.Labels == null || task.Labels.Count == 0)
            {
                return Array.Empty<string>();
            }

            return task.Labels
                .Select(l => l.Title)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task labels for task {TaskId}", taskId);
            return Array.Empty<string>();
        }
    }
}
