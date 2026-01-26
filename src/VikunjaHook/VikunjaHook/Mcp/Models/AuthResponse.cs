namespace VikunjaHook.Mcp.Models;

public record AuthResponse(
    string SessionId,
    string AuthType
);

public record ErrorMessage(
    string Error
);
