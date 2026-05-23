using LootNet_API.Services.Interfaces;

namespace LootNet_API.Services;

public class EmailVerificationCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailVerificationCleanupService> _logger;

    public EmailVerificationCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailVerificationCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                var deleted = await authService.DeleteExpiredUnverifiedUsersAsync();

                if (deleted > 0)
                    _logger.LogInformation("Deleted {Count} expired unverified users.", deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete expired unverified users.");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }
}
