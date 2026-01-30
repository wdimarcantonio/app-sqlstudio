using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Services;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        IWorkspaceManager workspaceManager,
        ILogger<SessionsController> logger)
    {
        _workspaceManager = workspaceManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all active session IDs
    /// </summary>
    [HttpGet]
    public IActionResult GetActiveSessions()
    {
        var sessions = _workspaceManager.GetActiveSessions();
        return Ok(new
        {
            sessions = sessions.ToList(),
            count = sessions.Count()
        });
    }

    /// <summary>
    /// Delete a specific session
    /// </summary>
    [HttpDelete("{sessionId}")]
    public IActionResult DeleteSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Session ID is required");
        }

        _workspaceManager.RemoveWorkspace(sessionId);
        _logger.LogInformation("Session {SessionId} deleted via API", sessionId);
        
        return Ok(new { message = $"Session {sessionId} deleted successfully" });
    }

    /// <summary>
    /// Manually trigger cleanup of inactive sessions
    /// </summary>
    [HttpPost("cleanup")]
    public IActionResult CleanupInactiveSessions([FromQuery] int inactivityMinutes = 30)
    {
        var threshold = TimeSpan.FromMinutes(inactivityMinutes);
        var removedCount = _workspaceManager.CleanupInactiveSessions(threshold);
        
        _logger.LogInformation("Manual cleanup removed {Count} inactive sessions", removedCount);
        
        return Ok(new
        {
            message = "Cleanup completed",
            removedCount = removedCount,
            inactivityThresholdMinutes = inactivityMinutes
        });
    }
}
