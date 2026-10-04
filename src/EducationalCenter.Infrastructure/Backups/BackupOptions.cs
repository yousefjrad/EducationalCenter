namespace EducationalCenter.Infrastructure.Backups;

/// <summary>Bound from the "Backup" configuration section.</summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// A folder on the machine where SQL Server runs, and SQL Server's service account must be able to write to it.
    /// Leave empty to use the instance's default backup folder, which always works.
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>Local time of the automatic daily backup, "HH:mm".</summary>
    public string DailyAtLocalTime { get; set; } = "02:00";

    /// <summary>Older backups are deleted, but the 3 newest are always kept.</summary>
    public int RetentionDays { get; set; } = 14;
}
