namespace EducationalCenter.Infrastructure.Persistence.Configurations;

internal static class Db
{
    /// <summary>Unique rules apply only to rows that are not soft-deleted, so a name can be reused after a delete.</summary>
    public const string NotDeleted = "[IsDeleted] = 0";
}
