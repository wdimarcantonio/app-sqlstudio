using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Services;

namespace SqlExcelBlazor.Server.Controllers;

/// <summary>
/// Controller per il monitoraggio e gestione delle sessioni
/// </summary>
[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IWorkspaceManager _workspaceManager;

    public SessionsController(IWorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager;
    }

    /// <summary>
    /// Ottiene informazioni sulle sessioni attive (utile per admin/debug)
    /// </summary>
    [HttpGet("active")]
    public IActionResult GetActiveSessions()
    {
        var workspaces = _workspaceManager.GetActiveWorkspaces();
        
        var info = workspaces.Select(w => new
        {
            SessionId = w.SessionId,
            CreatedAt = w.CreatedAt,
            LastActivity = w.LastActivity,
            ActiveFor = DateTime.UtcNow - w.CreatedAt,
            InactiveSince = DateTime.UtcNow - w.LastActivity
        }).ToList();

        return Ok(new 
        { 
            success = true, 
            count = info.Count,
            sessions = info 
        });
    }

    /// <summary>
    /// Ottiene info sulla sessione corrente
    /// </summary>
    [HttpGet("current")]
    public IActionResult GetCurrentSession()
    {
        var sessionId = HttpContext.Connection.Id ?? "default";
        var workspace = _workspaceManager.GetWorkspace(sessionId);

        if (workspace == null)
            return Ok(new { success = true, hasWorkspace = false, sessionId });

        return Ok(new
        {
            success = true,
            hasWorkspace = true,
            sessionId = workspace.SessionId,
            createdAt = workspace.CreatedAt,
            lastActivity = workspace.LastActivity
        });
    }
}
