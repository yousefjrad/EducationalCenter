using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Application.Features.StudentPortal;
using EducationalCenter.Application.Features.PublicCatalog;
using EducationalCenter.Infrastructure.Backups;
using EducationalCenter.Infrastructure.BackgroundJobs;
using EducationalCenter.Infrastructure.Common;
using EducationalCenter.Infrastructure.Documents;
using EducationalCenter.Infrastructure.Persistence;
using EducationalCenter.Infrastructure.Persistence.Interceptors;
using EducationalCenter.Infrastructure.Persistence.Repositories;
using EducationalCenter.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationalCenter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The connection string 'DefaultConnection' is missing.");

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

        if (jwt.SecretKey.Length < JwtOptions.MinimumSecretLength)
            throw new InvalidOperationException(
                $"Jwt:SecretKey must be at least {JwtOptions.MinimumSecretLength} characters. " +
                "Set it with 'dotnet user-secrets' or an environment variable; never commit it.");

        if (jwt.AccessTokenMinutes <= 0)
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be greater than zero.");

        var backup = configuration.GetSection(BackupOptions.SectionName).Get<BackupOptions>() ?? new BackupOptions();
        if (backup.RetentionDays < 1)
            throw new InvalidOperationException("Backup:RetentionDays must be at least 1.");

        services.AddSingleton(jwt);
        services.AddSingleton(backup);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();

        services.AddScoped<AuditableEntitiesInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            DbOptions.Configure(options, connectionString);
            options.AddInterceptors(provider.GetRequiredService<AuditableEntitiesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISettingsProvider, DatabaseSettingsProvider>();
        services.AddScoped<IReceiptNumberGenerator, ReceiptNumberGenerator>();
        services.AddScoped<IStudentPortalReader, StudentPortalReader>();
        services.AddScoped<IPublicCatalogReader, PublicCatalogReader>();

        // PDF is parked (docs/PDF.md): these stand-ins report "not installed" instead of failing the build.
        services.AddSingleton<IReceiptPdfGenerator, QuestPdfReceiptGenerator>();
        services.AddSingleton<ICertificatePdfGenerator, QuestPdfCertificateGenerator>();
        services.AddSingleton<IReportExporter, ReportExporter>();
        services.AddSingleton<IExcelService, ExcelService>();

        services.AddScoped<SqlServerBackupService>();
        services.AddScoped<IBackupService>(provider => provider.GetRequiredService<SqlServerBackupService>());

        services.AddHostedService<ExpireEnrollmentHoldsService>();
        if (backup.Enabled)
            services.AddHostedService<DatabaseBackupService>();

        // Logging (Serilog) is configured on the host in the next phase (Program.cs).

        return services;
    }
}
