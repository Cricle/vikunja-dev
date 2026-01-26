using System.Text.Json;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Models.Requests;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Tools;

/// <summary>
/// MCP tool for managing Vikunja projects
/// </summary>
public class ProjectsTool : IMcpTool
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly IAuthenticationManager _authManager;
    private readonly ILogger<ProjectsTool> _logger;

    public string Name => "projects";
    public string Description => "Manage Vikunja projects: create, get, update, delete, list, archive, unarchive";

    public IReadOnlyList<string> Subcommands => new[]
    {
        "list",
        "create",
        "get",
        "update",
        "delete",
        "archive",
        "unarchive",
        "get-children",
        "get-tree",
        "get-breadcrumb",
        "move"
    };

    public ProjectsTool(
        IVikunjaClientFactory clientFactory,
        IAuthenticationManager authManager,
        ILogger<ProjectsTool> logger)
    {
        _clientFactory = clientFactory;
        _authManager = authManager;
        _logger = logger;
    }

    public async Task<object?> ExecuteAsync(
        string subcommand,
        Dictionary<string, object?> parameters,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = _authManager.GetSession(sessionId);
        if (session == null)
        {
            throw new AuthenticationException("Invalid session");
        }

        return subcommand switch
        {
            "list" => await ListProjectsAsync(session, parameters, cancellationToken),
            "create" => await CreateProjectAsync(session, parameters, cancellationToken),
            "get" => await GetProjectAsync(session, parameters, cancellationToken),
            "update" => await UpdateProjectAsync(session, parameters, cancellationToken),
            "delete" => await DeleteProjectAsync(session, parameters, cancellationToken),
            "archive" => await ArchiveProjectAsync(session, parameters, cancellationToken),
            "unarchive" => await UnarchiveProjectAsync(session, parameters, cancellationToken),
            "get-children" => await GetProjectChildrenAsync(session, parameters, cancellationToken),
            "get-tree" => await GetProjectTreeAsync(session, parameters, cancellationToken),
            "get-breadcrumb" => await GetProjectBreadcrumbAsync(session, parameters, cancellationToken),
            "move" => await MoveProjectAsync(session, parameters, cancellationToken),
            _ => throw new ValidationException($"Unknown subcommand: {subcommand}")
        };
    }

    private async Task<object> ListProjectsAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing projects for session {SessionId}", session.SessionId);

        // Extract query parameters
        var page = GetIntParameter(parameters, "page", 1);
        var perPage = GetIntParameter(parameters, "perPage", 50);
        var search = GetStringParameter(parameters, "search");
        var isArchived = GetBoolParameter(parameters, "isArchived");

        // Build query string
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

        var projects = await _clientFactory.GetAsync<List<VikunjaProject>>(
            session,
            endpoint,
            cancellationToken
        );

        return new
        {
            projects = projects ?? new List<VikunjaProject>(),
            count = projects?.Count ?? 0,
            page,
            perPage
        };
    }

    private async Task<object> CreateProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating project for session {SessionId}", session.SessionId);

        // Extract and validate required parameters
        var title = GetStringParameter(parameters, "title")
            ?? throw new ValidationException("title is required");

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("title cannot be empty");
        }

        // Extract optional parameters
        var description = GetStringParameter(parameters, "description");
        var hexColor = GetStringParameter(parameters, "hexColor");
        var parentProjectId = GetLongParameter(parameters, "parentProjectId");

        // Validate hex color if provided
        if (!string.IsNullOrWhiteSpace(hexColor))
        {
            ValidateHexColor(hexColor);
        }

        // Build request
        var request = new CreateProjectRequest(
            Title: title.Trim(),
            Description: description?.Trim(),
            HexColor: hexColor?.ToLowerInvariant(),
            ParentProjectId: parentProjectId
        );

        var project = await _clientFactory.PutAsync<VikunjaProject>(
            session,
            "projects",
            request,
            cancellationToken
        );

        return new
        {
            project,
            message = $"Project '{title}' created successfully"
        };
    }

    private async Task<object> GetProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Getting project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        var project = await _clientFactory.GetAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        if (project == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        return new { project };
    }

    private async Task<object> UpdateProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Updating project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        // Extract optional update fields
        var title = GetStringParameter(parameters, "title");
        var description = GetStringParameter(parameters, "description");
        var hexColor = GetStringParameter(parameters, "hexColor");
        var parentProjectId = GetLongParameter(parameters, "parentProjectId");

        // Check if at least one field to update is provided
        if (title == null && description == null && hexColor == null && parentProjectId == null)
        {
            throw new ValidationException("At least one field to update must be provided");
        }

        // Validate hex color if provided
        if (!string.IsNullOrWhiteSpace(hexColor))
        {
            ValidateHexColor(hexColor);
        }

        // Build update request with only provided fields
        var updateData = new Dictionary<string, object?>();

        if (title != null)
        {
            updateData["title"] = title.Trim();
        }

        if (description != null)
        {
            updateData["description"] = description.Trim();
        }

        if (hexColor != null)
        {
            updateData["hex_color"] = hexColor.ToLowerInvariant();
        }

        if (parentProjectId.HasValue)
        {
            updateData["parent_project_id"] = parentProjectId.Value;
        }

        var project = await _clientFactory.PostAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            updateData,
            cancellationToken
        );

        return new
        {
            project,
            message = "Project updated successfully"
        };
    }

    private async Task<object> DeleteProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Deleting project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        // Get project details before deletion
        var project = await _clientFactory.GetAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        if (project == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        await _clientFactory.DeleteAsync(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        return new
        {
            message = $"Deleted project: {project.Title}",
            deleted = true,
            projectId,
            projectTitle = project.Title
        };
    }

    private async Task<object> ArchiveProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Archiving project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        // Get current project first
        var currentProject = await _clientFactory.GetAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        if (currentProject == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        // Check if project is already archived
        if (currentProject.IsArchived)
        {
            return new
            {
                project = currentProject,
                message = $"Project '{currentProject.Title}' is already archived"
            };
        }

        // Archive the project
        var updateData = new Dictionary<string, object?>
        {
            ["title"] = currentProject.Title,
            ["is_archived"] = true
        };

        var project = await _clientFactory.PutAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            updateData,
            cancellationToken
        );

        return new
        {
            project,
            message = $"Project '{project?.Title}' archived successfully"
        };
    }

    private async Task<object> UnarchiveProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Unarchiving project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        // Get current project first
        var currentProject = await _clientFactory.GetAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        if (currentProject == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        // Check if project is already active (not archived)
        if (!currentProject.IsArchived)
        {
            return new
            {
                project = currentProject,
                message = $"Project '{currentProject.Title}' is already active (not archived)"
            };
        }

        // Unarchive the project
        var updateData = new Dictionary<string, object?>
        {
            ["title"] = currentProject.Title,
            ["is_archived"] = false
        };

        var project = await _clientFactory.PutAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            updateData,
            cancellationToken
        );

        return new
        {
            project,
            message = $"Project '{project?.Title}' unarchived successfully"
        };
    }

    // Helper methods for parameter extraction
    private static string? GetStringParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            return value is JsonElement element && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : value.ToString();
        }
        return null;
    }

    private static long? GetLongParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var num))
                    return num;
            }
            else if (value is long l)
                return l;
            else if (value is int i)
                return i;
            else if (long.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static int? GetIntParameter(Dictionary<string, object?> parameters, string key, int? defaultValue = null)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var num))
                    return num;
            }
            else if (value is int i)
                return i;
            else if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    private static bool? GetBoolParameter(Dictionary<string, object?> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value) && value != null)
        {
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    return element.GetBoolean();
            }
            else if (value is bool b)
                return b;
            else if (bool.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private async Task<object> GetProjectChildrenAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var includeArchived = GetBoolParameter(parameters, "includeArchived") ?? false;

        _logger.LogInformation("Getting children of project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        // Verify the project exists
        var project = await _clientFactory.GetAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        if (project == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        // Get all projects and filter for children
        var allProjects = await _clientFactory.GetAsync<List<VikunjaProject>>(
            session,
            "projects?per_page=1000",
            cancellationToken
        ) ?? new List<VikunjaProject>();

        var children = allProjects
            .Where(p => p.ParentProjectId == projectId)
            .Where(p => includeArchived || !p.IsArchived)
            .ToList();

        return new
        {
            parentId = projectId,
            parentTitle = project.Title,
            children,
            count = children.Count,
            message = $"Found {children.Count} child project(s) for project '{project.Title}'"
        };
    }

    private async Task<object> GetProjectTreeAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        var maxDepth = GetIntParameter(parameters, "maxDepth") ?? 10;
        var includeArchived = GetBoolParameter(parameters, "includeArchived") ?? false;

        if (maxDepth < 1 || maxDepth > 10)
        {
            throw new ValidationException("maxDepth must be between 1 and 10");
        }

        _logger.LogInformation("Getting tree for project {ProjectId} with maxDepth {MaxDepth} for session {SessionId}",
            projectId, maxDepth, session.SessionId);

        // Get all projects
        var allProjects = await _clientFactory.GetAsync<List<VikunjaProject>>(
            session,
            "projects?per_page=1000",
            cancellationToken
        ) ?? new List<VikunjaProject>();

        // Find the root project
        var rootProject = allProjects.FirstOrDefault(p => p.Id == projectId);
        if (rootProject == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        // Build the tree
        var tree = BuildProjectTree(rootProject, allProjects, 0, maxDepth, includeArchived);
        var totalNodes = tree != null ? CountTreeNodes(tree) : 0;
        var actualDepth = tree != null ? GetTreeDepth(tree) : 0;

        return new
        {
            rootId = projectId,
            rootTitle = rootProject.Title,
            tree,
            totalNodes,
            actualDepth,
            maxDepth,
            message = $"Project tree for '{rootProject.Title}' contains {totalNodes} node(s) with depth {actualDepth}"
        };
    }

    private async Task<object> GetProjectBreadcrumbAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");

        _logger.LogInformation("Getting breadcrumb for project {ProjectId} for session {SessionId}",
            projectId, session.SessionId);

        // Get all projects
        var allProjects = await _clientFactory.GetAsync<List<VikunjaProject>>(
            session,
            "projects?per_page=1000",
            cancellationToken
        ) ?? new List<VikunjaProject>();

        var targetProject = allProjects.FirstOrDefault(p => p.Id == projectId);
        if (targetProject == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        // Build breadcrumb path
        var breadcrumb = BuildBreadcrumb(projectId, allProjects);

        return new
        {
            projectId,
            projectTitle = targetProject.Title,
            breadcrumb,
            depth = breadcrumb.Count - 1,
            message = $"Breadcrumb path: {string.Join(" > ", breadcrumb.Select(p => p.Title))}"
        };
    }

    private async Task<object> MoveProjectAsync(
        AuthSession session,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var projectId = GetLongParameter(parameters, "id")
            ?? throw new ValidationException("id is required");
        
        // parentProjectId can be null (move to root) or a valid project ID
        long? newParentId = null;
        if (parameters.TryGetValue("parentProjectId", out var parentValue) && parentValue != null)
        {
            newParentId = GetLongParameter(parameters, "parentProjectId");
        }

        _logger.LogInformation("Moving project {ProjectId} to parent {ParentId} for session {SessionId}",
            projectId, newParentId?.ToString() ?? "root", session.SessionId);

        // Get current project
        var currentProject = await _clientFactory.GetAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            cancellationToken
        );

        if (currentProject == null)
        {
            throw new ResourceNotFoundException("Project", projectId);
        }

        // Get all projects for validation
        var allProjects = await _clientFactory.GetAsync<List<VikunjaProject>>(
            session,
            "projects?per_page=1000",
            cancellationToken
        ) ?? new List<VikunjaProject>();

        // Validate move constraints
        ValidateMoveConstraints(projectId, newParentId, allProjects);

        // Perform the move
        var updateData = new Dictionary<string, object?>
        {
            ["title"] = currentProject.Title
        };

        if (newParentId.HasValue)
        {
            updateData["parent_project_id"] = newParentId.Value;
        }
        else
        {
            updateData["parent_project_id"] = null;
        }

        var updatedProject = await _clientFactory.PutAsync<VikunjaProject>(
            session,
            $"projects/{projectId}",
            updateData,
            cancellationToken
        );

        var parentInfo = newParentId.HasValue
            ? $" to parent project {newParentId.Value}"
            : " to root level";

        return new
        {
            project = updatedProject,
            oldParentProjectId = currentProject.ParentProjectId,
            newParentProjectId = newParentId,
            message = $"Moved project '{currentProject.Title}'{parentInfo}"
        };
    }

    // Helper methods for hierarchy operations
    private class ProjectTreeNode
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? HexColor { get; set; }
        public bool IsArchived { get; set; }
        public long? ParentProjectId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public int Depth { get; set; }
        public List<ProjectTreeNode> Children { get; set; } = new();
    }

    private static ProjectTreeNode? BuildProjectTree(
        VikunjaProject project,
        List<VikunjaProject> allProjects,
        int currentDepth,
        int maxDepth,
        bool includeArchived)
    {
        if (currentDepth >= maxDepth)
        {
            return null;
        }

        var children = allProjects
            .Where(p => p.ParentProjectId == project.Id)
            .Where(p => includeArchived || !p.IsArchived)
            .Select(child => BuildProjectTree(child, allProjects, currentDepth + 1, maxDepth, includeArchived))
            .Where(node => node != null)
            .Cast<ProjectTreeNode>()
            .ToList();

        return new ProjectTreeNode
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            HexColor = project.HexColor,
            IsArchived = project.IsArchived,
            ParentProjectId = project.ParentProjectId,
            Created = project.Created,
            Updated = project.Updated,
            Depth = currentDepth,
            Children = children
        };
    }

    private static int CountTreeNodes(ProjectTreeNode node)
    {
        return 1 + node.Children.Sum(child => CountTreeNodes(child));
    }

    private static int GetTreeDepth(ProjectTreeNode node)
    {
        if (node.Children.Count == 0)
        {
            return node.Depth;
        }
        return node.Children.Max(child => GetTreeDepth(child));
    }

    private static List<VikunjaProject> BuildBreadcrumb(long targetId, List<VikunjaProject> allProjects)
    {
        var breadcrumb = new List<VikunjaProject>();
        var visited = new HashSet<long>();
        long? currentId = targetId;

        while (currentId.HasValue)
        {
            if (visited.Contains(currentId.Value))
            {
                throw new McpException(
                    McpErrorCode.OperationFailed,
                    "Circular reference detected in project hierarchy while building breadcrumb"
                );
            }

            var project = allProjects.FirstOrDefault(p => p.Id == currentId.Value);
            if (project == null)
            {
                break;
            }

            visited.Add(currentId.Value);
            breadcrumb.Insert(0, project);
            currentId = project.ParentProjectId;
        }

        return breadcrumb;
    }

    private static void ValidateMoveConstraints(long projectId, long? newParentId, List<VikunjaProject> allProjects)
    {
        // Cannot move a project to itself
        if (newParentId == projectId)
        {
            throw new ValidationException("Cannot move a project to itself");
        }

        // If moving to a parent, validate it exists
        if (newParentId.HasValue)
        {
            var parentProject = allProjects.FirstOrDefault(p => p.Id == newParentId.Value);
            if (parentProject == null)
            {
                throw new ResourceNotFoundException("Parent project", newParentId.Value);
            }

            // Check for circular reference: ensure new parent is not a descendant of the project being moved
            if (IsDescendant(newParentId.Value, projectId, allProjects))
            {
                throw new ValidationException("Cannot move a project to one of its descendants (would create circular reference)");
            }

            // Check depth constraint
            var newDepth = CalculateProjectDepth(newParentId.Value, allProjects) + 1;
            var subtreeDepth = CalculateSubtreeDepth(projectId, allProjects);
            var totalDepth = newDepth + subtreeDepth;

            if (totalDepth > 10)
            {
                throw new ValidationException($"Move would exceed maximum depth of 10 levels (would result in depth {totalDepth})");
            }
        }
    }

    private static bool IsDescendant(long potentialDescendantId, long ancestorId, List<VikunjaProject> allProjects)
    {
        var visited = new HashSet<long>();
        long? currentId = potentialDescendantId;

        while (currentId.HasValue)
        {
            if (visited.Contains(currentId.Value))
            {
                return false; // Circular reference, but not a descendant
            }

            if (currentId.Value == ancestorId)
            {
                return true;
            }

            visited.Add(currentId.Value);
            var project = allProjects.FirstOrDefault(p => p.Id == currentId.Value);
            currentId = project?.ParentProjectId;
        }

        return false;
    }

    private static int CalculateProjectDepth(long projectId, List<VikunjaProject> allProjects)
    {
        var depth = 0;
        var visited = new HashSet<long>();
        long? currentId = projectId;

        while (currentId.HasValue)
        {
            if (visited.Contains(currentId.Value))
            {
                throw new McpException(
                    McpErrorCode.OperationFailed,
                    "Circular reference detected in project hierarchy"
                );
            }

            visited.Add(currentId.Value);
            var project = allProjects.FirstOrDefault(p => p.Id == currentId.Value);
            if (project == null)
            {
                break;
            }

            currentId = project.ParentProjectId;
            if (currentId.HasValue)
            {
                depth++;
            }
        }

        return depth;
    }

    private static int CalculateSubtreeDepth(long projectId, List<VikunjaProject> allProjects)
    {
        var children = allProjects.Where(p => p.ParentProjectId == projectId).ToList();
        if (children.Count == 0)
        {
            return 0;
        }

        return 1 + children.Max(child => CalculateSubtreeDepth(child.Id, allProjects));
    }

    private static void ValidateHexColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            throw new ValidationException("Hex color cannot be empty");
        }

        // Must start with # and be followed by exactly 6 hex digits
        if (!System.Text.RegularExpressions.Regex.IsMatch(hexColor, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new ValidationException($"Invalid hex color format: {hexColor}. Expected format: #RRGGBB");
        }
    }
}
