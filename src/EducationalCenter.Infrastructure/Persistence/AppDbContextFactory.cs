using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EducationalCenter.Infrastructure.Persistence;

/// <summary>
/// Lets "dotnet ef" create the context without starting the API.
/// Set EDUCATIONAL_CENTER_CONNECTION to use another database than the local default.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnection =
        "Server=.;Database=EducationalCenter;Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EDUCATIONAL_CENTER_CONNECTION") ?? DefaultConnection;

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        DbOptions.Configure(builder, connectionString);

        return new AppDbContext(builder.Options);
    }
}
