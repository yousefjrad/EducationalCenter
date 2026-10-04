using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Domain.Constants;
using EducationalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Infrastructure.Persistence;

/// <summary>
/// Run once at application start (Program.cs, next phase). Safe to run on every start:
/// it applies pending migrations and only adds what is missing.
/// </summary>
public static class DatabaseInitializer
{
    private const string ArabicCertificateBody =
        "تشهد {CenterName} بأن الطالب {StudentName} قد أتمّ بنجاح دورة {CourseName} (الشعبة {SectionName}). " +
        "صدرت هذه الشهادة بتاريخ {IssueDate} برقم {CertificateNumber}.";

    private const string EnglishCertificateBody =
        "{CenterName} certifies that {StudentName} has successfully completed the course {CourseName} " +
        "(section {SectionName}). Issued on {IssueDate}, certificate no. {CertificateNumber}.";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync(ct);

        await SeedPermissionsAsync(db, ct);
        await SeedRolesAsync(db, ct);
        await SeedSettingsAsync(db, ct);
        await SeedCertificateTemplatesAsync(db, ct);
        await SeedFirstAdminAsync(db, hasher, configuration, ct);
    }

    /// <summary>The code is the source of truth: every permission it defines exists in the database.</summary>
    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Name).ToListAsync(ct);
        var missing = Permissions.All.Except(existing).ToList();

        if (missing.Count == 0)
            return;

        db.Permissions.AddRange(missing.Select(name => new Permission { Name = name }));
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        var allPermissions = await db.Permissions.ToListAsync(ct);
        var permissionsByName = allPermissions.ToDictionary(p => p.Name);

        // Admin: always holds every permission, including ones added by later versions.
        var admin = await db.Roles.Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == SystemRoles.Admin, ct);

        if (admin is null)
        {
            admin = new Role { Name = SystemRoles.Admin, Description = "Full access to everything", IsSystem = true };
            db.Roles.Add(admin);
        }

        var adminHas = admin.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var permission in allPermissions.Where(p => !adminHas.Contains(p.Id)))
            admin.RolePermissions.Add(new RolePermission { Role = admin, Permission = permission });

        // Receptionist: created once with its defaults. After that the Admin may edit it freely,
        // so it is never overwritten here.
        var receptionistExists = await db.Roles.AnyAsync(r => r.Name == SystemRoles.Receptionist, ct);
        if (!receptionistExists)
        {
            var receptionist = new Role
            {
                Name = SystemRoles.Receptionist,
                Description = "Front desk: students, enrollment, payments and daily operations",
                IsSystem = true
            };

            foreach (var name in SystemRoles.ReceptionistPermissions)
                receptionist.RolePermissions.Add(new RolePermission { Role = receptionist, Permission = permissionsByName[name] });

            db.Roles.Add(receptionist);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Settings.Select(s => s.Key).ToListAsync(ct);

        var missing = SettingDefaults.All.Where(d => !existing.Contains(d.Key)).ToList();
        if (missing.Count == 0)
            return;

        db.Settings.AddRange(missing.Select(d => new Setting { Key = d.Key, Value = d.Value, Description = d.Description }));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>So certificates work from day one. The Admin edits the names and wording afterwards.</summary>
    private static async Task SeedCertificateTemplatesAsync(AppDbContext db, CancellationToken ct)
    {
        foreach (var (language, body, signer) in new[]
                 {
                     ("ar", ArabicCertificateBody, "مدير المركز"),
                     ("en", EnglishCertificateBody, "Center Director")
                 })
        {
            if (await db.CertificateTemplates.AnyAsync(t => t.Language == language, ct))
                continue;

            db.CertificateTemplates.Add(new CertificateTemplate
            {
                CenterName = "Educational Center",
                SignerName = signer,
                SignerTitle = signer,
                BodyText = body,
                Language = language,
                IsDefault = true
            });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Only when no user exists yet. The email and password come from configuration
    /// (user-secrets or environment variables) and are never written in the code or the repository.
    /// </summary>
    private static async Task SeedFirstAdminAsync(
        AppDbContext db, IPasswordHasher hasher, IConfiguration configuration, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
            return;

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "No users exist yet. Set Seed:AdminEmail and Seed:AdminPassword (for example with " +
                "'dotnet user-secrets') so the first Admin can be created.");

        if (password.Length < 8)
            throw new InvalidOperationException("Seed:AdminPassword must be at least 8 characters.");

        var adminRole = await db.Roles.FirstAsync(r => r.Name == SystemRoles.Admin, ct);

        db.Users.Add(new User
        {
            FullName = configuration["Seed:AdminName"] ?? "Administrator",
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = hasher.Hash(password),
            RoleId = adminRole.Id,
            IsActive = true
        });

        await db.SaveChangesAsync(ct);
    }
}
