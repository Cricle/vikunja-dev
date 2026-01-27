namespace Vikunja.Core.Mcp.Models;

/// <summary>
/// Session info for admin API
/// </summary>
public record SessionInfo(
    string SessionId,
    string ApiUrl,
    string AuthType,
    DateTime Created,
    DateTime LastAccessed,
    bool IsExpired
);

/// <summary>
/// Sessions list response
/// </summary>
public record SessionsResponse(
    List<SessionInfo> Sessions,
    int Count
);

/// <summary>
/// Server statistics response
/// </summary>
public record ServerStatsResponse(
    ServerInfoStats Server,
    SessionStats Sessions,
    ToolStats Tools,
    MemoryStats Memory
);

public record ServerInfoStats(
    string Name,
    string Version,
    string Uptime
);

public record SessionStats(
    int Total,
    int Active
);

public record ToolStats(
    int Total,
    int Subcommands
);

public record MemoryStats(
    long WorkingSet,
    long PrivateMemory
);

/// <summary>
/// Tool execution response for admin
/// </summary>
public record AdminToolExecutionResponse(
    bool Success,
    string Tool,
    string Subcommand,
    object? Data
);

/// <summary>
/// Log entry for admin API
/// </summary>
public record LogEntryInfo(
    string Timestamp,
    string Level,
    string Message
);

/// <summary>
/// Logs response
/// </summary>
public record LogsResponse(
    List<LogEntryInfo> Logs,
    int Count
);

/// <summary>
/// Simple message response
/// </summary>
public record MessageResponse(
    string Message
);
