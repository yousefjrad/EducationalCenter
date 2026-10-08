using System.Text.Json;
using EducationalCenter.Application;
using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Infrastructure;
using EducationalCenter.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EducationalCenter.Tests.Integration;

/// <summary>Real time plus an offset, so a test can jump forward (for example past a hold expiry) and reset.</summary>
public sealed class FakeClock : IClock
{
    private TimeSpan _offset = TimeSpan.Zero;

    public DateTime UtcNow => DateTime.UtcNow + _offset;

    public void Jump(DateTime to) => _offset = to - DateTime.UtcNow;

    public void Reset() => _offset = TimeSpan.Zero;
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public int? UserId { get; set; }

    public bool IsAuthenticated => UserId.HasValue;
}

/// <summary>One temporary database for the whole integration run. It is created by the real initializer and dropped at the end.</summary>
public sealed class AppFixture : IAsyncLifetime
{
    private ServiceProvider? _provider;

    public FakeClock Clock { get; } = new();

    public FakeSecurityEvents Security { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public AsyncServiceScope NewScope() => _provider!.CreateAsyncScope();

    public async Task InitializeAsync()
    {
        var connection = new SqlConnectionStringBuilder(FindConnectionString())
        {
            InitialCatalog = "EducationalCenter_Tests_" + Guid.NewGuid().ToString("N")[..8]
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString,
                ["Jwt:Issuer"] = "tests",
                ["Jwt:Audience"] = "tests",
                ["Jwt:SecretKey"] = new string('k', 80),
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Backup:Enabled"] = "false",
                ["Seed:AdminEmail"] = "admin@tests.local",
                ["Seed:AdminPassword"] = "Tests12345"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton<ISecurityEventLogger>(Security);
        services.RemoveAll<IClock>();
        services.AddSingleton<IClock>(Clock);
        services.RemoveAll<ICurrentUser>();
        services.AddSingleton<ICurrentUser>(CurrentUser);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await DatabaseInitializer.InitializeAsync(_provider);

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        CurrentUser.UserId = await db.Users.Select(u => u.Id).FirstAsync();
    }

    public async Task DisposeAsync()
    {
        if (_provider is null)
            return;

        await using (var scope = _provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
        }

        await _provider.DisposeAsync();
    }

    private static string FindConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var apiDirectory = Path.Combine(dir.FullName, "src", "EducationalCenter.API");

            foreach (var file in new[] { "appsettings.Development.json", "appsettings.json" })
            {
                var path = Path.Combine(apiDirectory, file);
                if (!File.Exists(path))
                    continue;

                using var document = JsonDocument.Parse(File.ReadAllText(path), options);
                if (document.RootElement.TryGetProperty("ConnectionStrings", out var section) &&
                    section.TryGetProperty("DefaultConnection", out var value) &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                    return value.GetString()!;
            }
        }

        throw new InvalidOperationException(
            "Could not find ConnectionStrings:DefaultConnection in src/EducationalCenter.API/appsettings*.json. " +
            "Set the TEST_SQL_CONNECTION environment variable instead.");
    }
}

[CollectionDefinition("integration")]
public sealed class IntegrationCollection : ICollectionFixture<AppFixture>;