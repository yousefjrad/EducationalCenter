namespace EducationalCenter.Application.Common.Models;

/// <param name="FilePath">The path on the SQL Server machine.</param>
public sealed record BackupInfo(string FilePath, DateTime FinishedAtUtc, long SizeBytes);
