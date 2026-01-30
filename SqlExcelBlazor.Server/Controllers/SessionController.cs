using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Services;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly ILogger<SessionController> _logger;

    public SessionController(WorkspaceManager workspaceManager, ILogger<SessionController> logger)
    {
        _workspaceManager = workspaceManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new session
    /// </summary>
    [HttpPost("create")]
    public IActionResult CreateSession([FromBody] CreateSessionRequest? request = null)
    {
        try
        {
            var session = _workspaceManager.CreateSession(request?.UserId);
            return Ok(new CreateSessionResponse
            {
                Success = true,
                SessionId = session.SessionId,
                CreatedAt = session.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Gets session information
    /// </summary>
    [HttpGet("{sessionId}")]
    public IActionResult GetSession(string sessionId)
    {
        var session = _workspaceManager.GetSession(sessionId);
        if (session == null)
        {
            return NotFound(new { success = false, error = "Session not found" });
        }

        return Ok(new SessionInfoResponse
        {
            Success = true,
            SessionId = session.SessionId,
            UserId = session.UserId,
            CreatedAt = session.CreatedAt,
            LastAccessedAt = session.LastAccessedAt,
            LoadedTables = session.LoadedTables.ToList()
        });
    }

    /// <summary>
    /// Gets all active sessions
    /// </summary>
    [HttpGet]
    public IActionResult GetActiveSessions()
    {
        var sessions = _workspaceManager.GetActiveSessions();
        return Ok(new
        {
            success = true,
            sessions = sessions,
            count = sessions.Count
        });
    }

    /// <summary>
    /// Closes a session
    /// </summary>
    [HttpDelete("{sessionId}")]
    public IActionResult CloseSession(string sessionId)
    {
        try
        {
            _workspaceManager.CloseSession(sessionId);
            return Ok(new { success = true, message = "Session closed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing session {SessionId}", sessionId);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Gets tables loaded in a session
    /// </summary>
    [HttpGet("{sessionId}/tables")]
    public IActionResult GetSessionTables(string sessionId)
    {
        var tables = _workspaceManager.GetLoadedTables(sessionId);
        if (tables == null)
        {
            return NotFound(new { success = false, error = "Session not found" });
        }

        return Ok(new { success = true, tables = tables });
    }
}

// Request/Response DTOs
public class CreateSessionRequest
{
    public string? UserId { get; set; }
}

public class CreateSessionResponse
{
    public bool Success { get; set; }
    public string SessionId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SessionInfoResponse
{
    public bool Success { get; set; }
    public string SessionId { get; set; } = "";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public List<string> LoadedTables { get; set; } = new();
}
