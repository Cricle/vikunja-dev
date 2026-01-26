using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VikunjaHook.Mcp.Models;
using VikunjaHook.Mcp.Services;

namespace VikunjaHook.Mcp.Controllers;

/// <summary>
/// Admin controller for managing MCP server
/// </summary>
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IAuthenticationManager _authManager;
    private readonly IToolRegistry _toolRegistry;
    private readonly IMcpServer _mcpServer;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAuthenticationManager authManager,
        IToolRegistry toolRegistry,
        IMcpServer mcpServer,
        ILogger<AdminController> logger)
    {
        _authManager = authManager;
        _toolRegistry = toolRegistry;
        _mcpServer = mcpServer;
        _logger = logger;
    }

    /// <summary>
    /// Get all active sessions
    /// </summary>
    [HttpGet("sessions")]
    public IActionResult GetSessions()
    {
        try
        {
            var sessions = _authManager.GetAllSessions();
            var sessionList = sessions.Select(s => new
            {
                sessionId = s.SessionId,
                apiUrl = s.ApiUrl,
                authType = s.AuthType.ToString(),
                created = s.Created,
                lastAccessed = s.LastAccessed,
                isExpired = s.IsExpired
            }).ToList();

            return Ok(new
            {
                sessions = sessionList,
                count = sessionList.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, new { error = "Failed to get sessions" });
        }
    }

    /// <summary>
    /// Disconnect a specific session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public IActionResult DisconnectSession(string sessionId)
    {
        try
        {
            var success = _authManager.Disconnect(sessionId);
            if (!success)
            {
                return NotFound(new { error = "Session not found" });
            }

            _logger.LogInformation("Session {SessionId} disconnected via admin", sessionId);
            return Ok(new { message = "Session disconnected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to disconnect session" });
        }
    }

    /// <summary>
    /// Disconnect all sessions
    /// </summary>
    [HttpDelete("sessions")]
    public IActionResult DisconnectAllSessions()
    {
        try
        {
            _authManager.DisconnectAll();
            _logger.LogInformation("All sessions disconnected via admin");
            return Ok(new { message = "All sessions disconnected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting all sessions");
            return StatusCode(500, new { error = "Failed to disconnect all sessions" });
        }
    }

    /// <summary>
    /// Get server statistics
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            var sessions = _authManager.GetAllSessions();
            var tools = _toolRegistry.GetAllTools();
            var serverInfo = _mcpServer.GetServerInfo();

            var stats = new
            {
                server = new
                {
                    name = serverInfo.Name,
                    version = serverInfo.Version,
                    uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
                },
                sessions = new
                {
                    total = sessions.Count,
                    active = sessions.Count(s => !s.IsExpired)
                },
                tools = new
                {
                    total = tools.Count,
                    subcommands = tools.Sum(t => t.Subcommands.Count)
                },
                memory = new
                {
                    workingSet = Process.GetCurrentProcess().WorkingSet64,
                    privateMemory = Process.GetCurrentProcess().PrivateMemorySize64
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, new { error = "Failed to get stats" });
        }
    }

    /// <summary>
    /// Execute a tool command (for testing)
    /// </summary>
    [HttpPost("tools/{toolName}/{subcommand}")]
    public async Task<IActionResult> ExecuteTool(
        string toolName,
        string subcommand,
        [FromBody] Dictionary<string, object?>? parameters,
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { error = "X-Session-Id header is required" });
            }

            if (!_authManager.IsAuthenticated(sessionId))
            {
                return Unauthorized(new { error = "Invalid or expired session" });
            }

            var tool = _toolRegistry.GetTool(toolName);
            if (tool == null)
            {
                return NotFound(new { error = $"Tool '{toolName}' not found" });
            }

            if (!tool.Subcommands.Contains(subcommand))
            {
                return BadRequest(new { error = $"Subcommand '{subcommand}' not found for tool '{toolName}'" });
            }

            parameters ??= new Dictionary<string, object?>();
            var result = await tool.ExecuteAsync(subcommand, parameters, sessionId);

            return Ok(new
            {
                success = true,
                tool = toolName,
                subcommand,
                data = result
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in tool execution");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {Tool}/{Subcommand}", toolName, subcommand);
            return StatusCode(500, new { error = "Failed to execute tool" });
        }
    }

    /// <summary>
    /// Get recent logs
    /// </summary>
    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int count = 100, [FromQuery] string? level = null)
    {
        try
        {
            // 读取最新的日志文件
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logsDir))
            {
                return Ok(new { logs = Array.Empty<object>(), count = 0 });
            }

            var logFiles = Directory.GetFiles(logsDir, "vikunja-mcp-*.log")
                .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                .Take(1)
                .ToList();

            if (logFiles.Count == 0)
            {
                return Ok(new { logs = Array.Empty<object>(), count = 0 });
            }

            var lines = System.IO.File.ReadLines(logFiles[0])
                .Reverse()
                .Take(count)
                .Reverse()
                .ToList();

            // 简单的日志解析
            var logs = lines.Select(line =>
            {
                var parts = line.Split(new[] { ' ' }, 4);
                if (parts.Length >= 4)
                {
                    return new
                    {
                        timestamp = $"{parts[0]} {parts[1]}",
                        level = parts[2].Trim('[', ']'),
                        message = parts[3]
                    };
                }
                return new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    level = "INFO",
                    message = line
                };
            }).ToList();

            // 按级别过滤
            if (!string.IsNullOrEmpty(level))
            {
                logs = logs.Where(l => l.level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(new { logs, count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs");
            return StatusCode(500, new { error = "Failed to get logs" });
        }
    }

    /// <summary>
    /// Clear all logs
    /// </summary>
    [HttpDelete("logs")]
    public IActionResult ClearLogs()
    {
        try
        {
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (Directory.Exists(logsDir))
            {
                foreach (var file in Directory.GetFiles(logsDir, "vikunja-mcp-*.log"))
                {
                    System.IO.File.Delete(file);
                }
            }

            _logger.LogInformation("Logs cleared via admin");
            return Ok(new { message = "Logs cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing logs");
            return StatusCode(500, new { error = "Failed to clear logs" });
        }
    }
}
