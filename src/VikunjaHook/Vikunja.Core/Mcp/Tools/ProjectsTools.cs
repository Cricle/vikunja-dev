using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Models.Requests;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// MCP tools for managing Vikunja projects
/// </summary>
public class ProjectsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<ProjectsTools> _logger;

    public ProjectsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<ProjectsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all projects")]
    public async Task<List<VikunjaProject>> ListProjects(
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        [Description("Search query (optional)")] string? search = null,
        [Description("Filter by archived status (optional)")] bool? isArchived = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing projects - page: {Page}", page);

        var queryParams = new List<string>
        {
            $"page={page}",
            $"per_page={perPage}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"s={Uri.EscapeDataString(search)}");
        }

        if (isArchived.HasValue)
        {
            queryParams.Add($"is_archived={isArchived.Value.ToString().ToLowerInvariant()}");
        }

        var endpoint = $"projects?{string.Join("&", queryParams)}";
        var projects = await _clientFactory.GetAsync<List<VikunjaProject>>(endpoint, cancellationToken);
        return projects ?? new List<VikunjaProject>();
    }

    [McpServerTool]
    [Description("Create a new project")]
    public async Task<VikunjaProject> CreateProject(
        [Description("Project title")] string title,
        [Description("Project description (optional)")] string? description = null,
        [Description("Hex color code (optional, e.g., #FF5733)")] string? hexColor = null,
        [Description("Parent project ID (optional)")] long? parentProjectId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating project '{Title}'", title);

        var request = new CreateProjectRequest(
            Title: title,
            Description: description,
            HexColor: hexColor?.ToLowerInvariant(),
            ParentProjectId: parentProjectId
        );

        var project = await _clientFactory.PutAsync<VikunjaProject>(
            "projects",
            request,
            cancellationToken
        );

        return project;
    }

    [McpServerTool]
    [Description("Get details of a specific project")]
    public async Task<VikunjaProject> GetProject(
        [Description("Project ID")] long projectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting project {ProjectId}", projectId);

        var project = await _clientFactory.GetAsync<VikunjaProject>(
            $"projects/{projectId}",
            cancellationToken
        );

        return project;
    }

    [McpServerTool]
    [Description("Update an existing project")]
    public async Task<VikunjaProject> UpdateProject(
        [Description("Project ID")] long projectId,
        [Description("New title (optional)")] string? title = null,
        [Description("New description (optional)")] string? description = null,
        [Description("New hex color code (optional)")] string? hexColor = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating project {ProjectId}", projectId);

        var request = new UpdateProjectRequest
        {
            Id = projectId,
            Title = title,
            Description = description,
            HexColor = hexColor?.ToLowerInvariant()
        };

        var project = await _clientFactory.PostAsync<VikunjaProject>(
            $"projects/{projectId}",
            request,
            cancellationToken
        );

        return project;
    }

    [McpServerTool]
    [Description("Delete a project")]
    public async Task<string> DeleteProject(
        [Description("Project ID")] long projectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting project {ProjectId}", projectId);

        await _clientFactory.DeleteAsync(
            $"projects/{projectId}",
            cancellationToken
        );

        return $"Project {projectId} deleted successfully";
    }
}
