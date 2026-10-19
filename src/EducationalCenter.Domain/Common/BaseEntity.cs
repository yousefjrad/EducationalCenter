namespace EducationalCenter.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Database version of the row (SQL Server rowversion). Changes on every update.</summary>
    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Not stored. The version the client last saw; when set before saving, an update is refused
    /// if someone else changed the row in the meantime.
    /// </summary>
    public byte[]? ExpectedRowVersion { get; set; }
}
