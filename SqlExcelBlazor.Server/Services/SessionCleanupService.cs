namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Background service per la pulizia automatica delle sessioni inattive
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupInterval, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var workspaceManager = scope.ServiceProvider.GetRequiredService<IWorkspaceManager>();

                var workspaces = workspaceManager.GetActiveWorkspaces();
                var inactiveWorkspaces = workspaces.Where(w => 
                    DateTime.UtcNow - w.LastActivity > _sessionTimeout
                ).ToList();

                foreach (var workspace in inactiveWorkspaces)
                {
                    try
                    {
                        await workspaceManager.CloseWorkspaceAsync(workspace.SessionId);
                        _logger.LogInformation($"Cleaned up inactive session {workspace.SessionId} (inactive for {DateTime.UtcNow - workspace.LastActivity})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to cleanup session {workspace.SessionId}");
                    }
                }

                if (inactiveWorkspaces.Any())
                {
                    _logger.LogInformation($"Session cleanup completed: {inactiveWorkspaces.Count} sessions closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session cleanup service");
            }
        }

        _logger.LogInformation("Session Cleanup Service stopped");
    }
}
