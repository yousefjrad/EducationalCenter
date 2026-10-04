using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Infrastructure.Common;
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

        services.AddSingleton(jwt);
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

        // PDF and Excel services, logging and background jobs are registered here in the next batches.

        return services;
    }
}
