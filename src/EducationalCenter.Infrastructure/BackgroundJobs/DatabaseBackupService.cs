using EducationalCenter.Infrastructure.Backups;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EducationalCenter.Infrastructure.BackgroundJobs;

/// <summary>Takes one verified backup a day at the configured local time, then removes old ones.</summary>
internal sealed class DatabaseBackupService(
    IServiceScopeFactory scopes,
    BackupOptions options,
    ILogger<DatabaseBackupService> logger) : BackgroundService
{
    private static readonly TimeOnly DefaultTime = new(2, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var time = TimeOnly.TryParse(options.DailyAtLocalTime, out var parsed) ? parsed : DefaultTime;

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNext(time);
            logger.LogInformation("Next automatic database backup in {Delay:hh\\:mm}.", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var backups = scope.ServiceProvider.GetRequiredService<SqlServerBackupService>();

            await backups.CreateBackupAsync(ct);
            await backups.PruneOldBackupsAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // Tomorrow's run still happens. The error stays in the log so it can be seen and fixed.
            logger.LogError(ex, "The automatic database backup failed.");
        }
    }

    private static TimeSpan DelayUntilNext(TimeOnly time)
    {
        var now = DateTime.Now;
        var next = now.Date.Add(time.ToTimeSpan());
        if (next <= now)
            next = next.AddDays(1);

        return next - now;
    }
}
