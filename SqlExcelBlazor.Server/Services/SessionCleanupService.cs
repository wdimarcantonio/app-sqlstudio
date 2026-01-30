namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Background service that periodically cleans up inactive sessions
/// Runs every 1 hour and removes sessions inactive for more than 2 hours
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromHours(2);

    public SessionCleanupService(
        ILogger<SessionCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                // Get WorkspaceManager from DI container
                using var scope = _serviceProvider.CreateScope();
                var workspaceManager = scope.ServiceProvider.GetRequiredService<WorkspaceManager>();

                var cleanedCount = workspaceManager.CleanupInactiveSessions(_inactivityThreshold);
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Session cleanup completed. Removed {Count} inactive sessions", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        _logger.LogInformation("SessionCleanupService stopped");
    }
}
