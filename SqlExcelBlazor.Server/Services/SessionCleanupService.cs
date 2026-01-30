namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Background service that periodically cleans up inactive sessions
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(30);

    public SessionCleanupService(
        IWorkspaceManager workspaceManager,
        ILogger<SessionCleanupService> logger)
    {
        _workspaceManager = workspaceManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                var removedCount = _workspaceManager.CleanupInactiveSessions(_inactivityThreshold);
                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} inactive sessions", removedCount);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }
}
