using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Tools;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public class McpToolsAdapter
{
    private readonly ProjectsTools _projectsTools;
    private readonly TasksTools _tasksTools;
    private readonly UsersTools _usersTools;
    private readonly ILogger<McpToolsAdapter> _logger;
    private readonly string? _vikunjaUrl;

    public McpToolsAdapter(
        ProjectsTools projectsTools,
        TasksTools tasksTools,
        UsersTools usersTools,
        ILogger<McpToolsAdapter> logger,
        string? vikunjaUrl = null)
    {
        _projectsTools = projectsTools;
        _tasksTools = tasksTools;
        _usersTools = usersTools;
        _logger = logger;
        _vikunjaUrl = vikunjaUrl?.TrimEnd('/');
    }

    public async Task<ProjectTemplateData?> GetProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var project = await _projectsTools.GetProject(projectId, cancellationToken);
            
            return new ProjectTemplateData
            {
                Id = (int)project.Id,
                Title = project.Title,
                Description = project.Description ?? string.Empty,
                Url = !string.IsNullOrEmpty(_vikunjaUrl) ? $"{_vikunjaUrl}/projects/{project.Id}" : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<TaskTemplateData?> GetTaskAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await _tasksTools.GetTask(taskId, cancellationToken);
            
            return new TaskTemplateData
            {
                Id = (int)task.Id,
                Title = task.Title,
                Description = task.Description ?? string.Empty,
                Done = task.Done,
                DueDate = task.DueDate?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                Priority = task.Priority,
                Url = !string.IsNullOrEmpty(_vikunjaUrl) ? $"{_vikunjaUrl}/tasks/{task.Id}" : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task {TaskId}", taskId);
            return null;
        }
    }

    public async Task<UserTemplateData?> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _usersTools.GetUser(userId, cancellationToken);
            
            return new UserTemplateData
            {
                Id = (int)user.Id,
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
