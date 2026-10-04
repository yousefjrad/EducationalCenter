using EducationalCenter.Application.Common.Models;

namespace EducationalCenter.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure (it is SQL Server specific). The API exposes it to the Admin.</summary>
public interface IBackupService
{
    /// <summary>Takes a full backup now and verifies it. Throws if SQL Server refuses or the file is damaged.</summary>
    Task<BackupInfo> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>The most recent backups of this database recorded by SQL Server, newest first.</summary>
    Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(int take = 20, CancellationToken ct = default);
}
