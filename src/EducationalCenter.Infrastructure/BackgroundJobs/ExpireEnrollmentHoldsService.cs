using EducationalCenter.Application.Features.Enrollments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EducationalCenter.Infrastructure.BackgroundJobs;

/// <summary>
/// Every few minutes, cancels temporary seat holds whose time ran out, so the seats are free again.
/// (Expired holds already stop counting as taken seats the moment they expire; this tidies their status.)
/// </summary>
internal sealed class ExpireEnrollmentHoldsService(
    IServiceScopeFactory scopes,
    ILogger<ExpireEnrollmentHoldsService> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Let the application finish its own startup (migrations, seeding) first.
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
            var enrollments = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

            var cancelled = await enrollments.ExpireHoldsAsync(ct);
            if (cancelled > 0)
                logger.LogInformation("Cancelled {Count} expired enrollment hold(s).", cancelled);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // One failed run must not stop the next ones.
            logger.LogError(ex, "Expiring enrollment holds failed.");
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
