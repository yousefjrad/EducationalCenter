using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Common.Models;
using EducationalCenter.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EducationalCenter.Infrastructure.Backups;

/// <summary>
/// Full backups with the native BACKUP DATABASE command. Each one is verified with RESTORE VERIFYONLY.
/// The file is written by SQL Server itself, which is why the default folder (always writable by it) is used
/// unless Backup:Directory says otherwise.
/// </summary>
internal sealed class SqlServerBackupService(
    AppDbContext db,
    BackupOptions options,
    IClock clock,
    ILogger<SqlServerBackupService> logger) : IBackupService
{
    private const int KeepNewestAlways = 3;

    // Express editions (and LocalDB) report engine edition 4 and do not support compressed backups.
    private const int ExpressEngineEdition = 4;

    private sealed class BackupRow
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime FinishedAt { get; set; }
        public long SizeBytes { get; set; }
    }

    public async Task<BackupInfo> CreateBackupAsync(CancellationToken ct = default)
    {
        var databaseName = db.Database.GetDbConnection().Database;
        var directory = await ResolveDirectoryAsync(ct);

        var path = Path.Combine(directory, $"{databaseName}_{clock.UtcNow:yyyyMMdd_HHmmss}.bak");

        var edition = await db.Database
            .SqlQueryRaw<int>("SELECT CAST(SERVERPROPERTY('EngineEdition') AS int) AS [Value]")
            .SingleAsync(ct);

        // The database name cannot be a parameter, so it is quoted instead.
        var quotedName = "[" + databaseName.Replace("]", "]]") + "]";
        var compression = edition == ExpressEngineEdition ? string.Empty : ", COMPRESSION";

        // Large databases can take a while.
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));

        #pragma warning disable EF1002 // The database name cannot be a SQL parameter: it is bracket-quoted above, the compression clause is a fixed value, and the file path is a real parameter.
        await db.Database.ExecuteSqlRawAsync(
            $"BACKUP DATABASE {quotedName} TO DISK = @path WITH INIT, CHECKSUM{compression}",
            new object[] { new SqlParameter("@path", path) },
            ct);
        #pragma warning restore EF1002

        await db.Database.ExecuteSqlRawAsync(
            "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM",
            new object[] { new SqlParameter("@path", path) },
            ct);

        logger.LogInformation("Database backup created and verified: {Path}", path);

        var recent = await ListBackupsAsync(1, ct);
        return recent.Count > 0 && string.Equals(recent[0].FilePath, path, StringComparison.OrdinalIgnoreCase)
            ? recent[0]
            : new BackupInfo(path, clock.UtcNow, 0);
    }

    public async Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(int take = 20, CancellationToken ct = default)
    {
        try
        {
            var rows = await db.Database.SqlQueryRaw<BackupRow>(
                    @"SELECT TOP (@take)
                             bmf.physical_device_name AS FilePath,
                             bs.backup_finish_date AS FinishedAt,
                             CAST(bs.backup_size AS bigint) AS SizeBytes
                      FROM msdb.dbo.backupset bs
                      JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
                      WHERE bs.database_name = DB_NAME() AND bs.type = 'D'
                      ORDER BY bs.backup_finish_date DESC",
                    new SqlParameter("@take", take))
                .ToListAsync(ct);

            // SQL Server stores its own local time.
            return rows
                .OrderByDescending(r => r.FinishedAt)
                .Select(r => new BackupInfo(r.FilePath, r.FinishedAt.ToUniversalTime(), r.SizeBytes))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The account may not be allowed to read msdb.
            logger.LogWarning(ex, "Could not read the backup history from msdb.");
            return [];
        }
    }

    /// <summary>
    /// Deletes this database's old backup files. Best effort: SQL Server owns the folder, so the
    /// application may not be allowed to delete there. The 3 newest backups are never touched.
    /// </summary>
    public async Task PruneOldBackupsAsync(CancellationToken ct = default)
    {
        var databaseName = db.Database.GetDbConnection().Database;
        var cutoff = clock.UtcNow.AddDays(-options.RetentionDays);

        var backups = await ListBackupsAsync(500, ct);

        foreach (var backup in backups.Skip(KeepNewestAlways).Where(b => b.FinishedAtUtc < cutoff))
        {
            var fileName = Path.GetFileName(backup.FilePath);

            if (!fileName.StartsWith(databaseName + "_", StringComparison.OrdinalIgnoreCase)
                || !fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(backup.FilePath))
                continue;

            try
            {
                File.Delete(backup.FilePath);
                logger.LogInformation("Deleted old backup: {Path}", backup.FilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete the old backup {Path}. Check the folder permissions.", backup.FilePath);
            }
        }
    }

    private async Task<string> ResolveDirectoryAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(options.Directory))
            return options.Directory;

        var defaultDirectory = await db.Database
            .SqlQueryRaw<string>("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(512)) AS [Value]")
            .SingleAsync(ct);

        return string.IsNullOrWhiteSpace(defaultDirectory)
            ? throw new InvalidOperationException("SQL Server has no default backup folder. Set Backup:Directory.")
            : defaultDirectory;
    }
}
