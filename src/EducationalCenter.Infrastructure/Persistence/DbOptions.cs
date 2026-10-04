using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EducationalCenter.Infrastructure.Persistence;

internal static class DbOptions
{
    /// <summary>Shared by the application and by the design-time factory used for migrations.</summary>
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString);

        // Child tables without their own soft-delete flag (AuditLog, Setting...) point at filtered parents on purpose.
        options.ConfigureWarnings(w =>
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }
}
