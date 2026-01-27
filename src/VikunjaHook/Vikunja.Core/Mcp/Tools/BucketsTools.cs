using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Vikunja.Core.Mcp.Services;

namespace Vikunja.Core.Mcp.Tools;

/// <summary>
/// Represents a bucket (kanban column) in Vikunja
/// </summary>
public record VikunjaBucket(
    long Id,
    string Title,
    long ProjectId,
    int Position,
    int Limit,
    int Count,
    DateTime Created,
    DateTime Updated
);

/// <summary>
/// MCP tools for managing buckets (kanban columns)
/// </summary>
public class BucketsTools
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly ILogger<BucketsTools> _logger;

    public BucketsTools(
        IVikunjaClientFactory clientFactory,
        ILogger<BucketsTools> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    [McpServerTool]
    [Description("List all buckets in a project")]
    public async Task<List<VikunjaBucket>> ListBuckets(
        [Description("Project ID")] long projectId,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Items per page (default: 50)")] int perPage = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing buckets for project {ProjectId}", projectId);

        var queryParams = $"page={page}&per_page={perPage}";
        var buckets = await _clientFactory.GetAsync<List<VikunjaBucket>>(
            $"projects/{projectId}/buckets?{queryParams}",
            cancellationToken
        );

        return buckets ?? new List<VikunjaBucket>();
    }

    [McpServerTool]
    [Description("Create a new bucket in a project")]
    public async Task<VikunjaBucket> CreateBucket(
        [Description("Project ID")] long projectId,
        [Description("Bucket title")] string title,
        [Description("Bucket limit (0 for unlimited)")] int limit = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating bucket '{Title}' in project {ProjectId}", title, projectId);

        var request = new
        {
            title,
            limit,
            project_id = projectId
        };

        var bucket = await _clientFactory.PutAsync<VikunjaBucket>(
            $"projects/{projectId}/buckets",
            request,
            cancellationToken
        );

        return bucket;
    }

    [McpServerTool]
    [Description("Get details of a specific bucket")]
    public async Task<VikunjaBucket> GetBucket(
        [Description("Project ID")] long projectId,
        [Description("Bucket ID")] long bucketId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting bucket {BucketId} from project {ProjectId}", bucketId, projectId);

        var bucket = await _clientFactory.GetAsync<VikunjaBucket>(
            $"projects/{projectId}/buckets/{bucketId}",
            cancellationToken
        );

        return bucket;
    }

    [McpServerTool]
    [Description("Update an existing bucket")]
    public async Task<VikunjaBucket> UpdateBucket(
        [Description("Project ID")] long projectId,
        [Description("Bucket ID")] long bucketId,
        [Description("New title (optional)")] string? title = null,
        [Description("New limit (optional)")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating bucket {BucketId} in project {ProjectId}", bucketId, projectId);

        var request = new
        {
            id = bucketId,
            title,
            limit
        };

        var bucket = await _clientFactory.PostAsync<VikunjaBucket>(
            $"projects/{projectId}/buckets/{bucketId}",
            request,
            cancellationToken
        );

        return bucket;
    }

    [McpServerTool]
    [Description("Delete a bucket")]
    public async Task<string> DeleteBucket(
        [Description("Project ID")] long projectId,
        [Description("Bucket ID")] long bucketId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting bucket {BucketId} from project {ProjectId}", bucketId, projectId);

        await _clientFactory.DeleteAsync(
            $"projects/{projectId}/buckets/{bucketId}",
            cancellationToken
        );

        return $"Bucket {bucketId} deleted from project {projectId}";
    }
}
