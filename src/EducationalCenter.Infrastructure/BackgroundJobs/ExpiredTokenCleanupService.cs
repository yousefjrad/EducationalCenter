using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EducationalCenter.Infrastructure.BackgroundJobs;

/// <summary>Once a day, deletes refresh tokens that expired more than 30 days ago so the table does not grow forever.</summary>
internal sealed class ExpiredTokenCleanupService(
    IServiceScopeFactory scopes,
    ILogger<ExpiredTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int KeepDaysAfterExpiry = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var cutoff = clock.UtcNow.AddDays(-KeepDaysAfterExpiry);

            var removed = await db.RefreshTokens.IgnoreQueryFilters()
                .Where(t => t.ExpiresAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (removed > 0)
                logger.LogInformation("Deleted {Count} expired refresh token(s).", removed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cleaning up expired refresh tokens failed.");
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}