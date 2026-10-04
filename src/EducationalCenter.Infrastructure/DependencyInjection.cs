using EducationalCenter.Application.Common.Interfaces;
using EducationalCenter.Infrastructure.Common;
using EducationalCenter.Infrastructure.Persistence;
using EducationalCenter.Infrastructure.Persistence.Interceptors;
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

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<AuditableEntitiesInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            DbOptions.Configure(options, connectionString);
            options.AddInterceptors(provider.GetRequiredService<AuditableEntitiesInterceptor>());
        });

        // Repositories, UnitOfWork and the other services are registered here in the next batches.

        return services;
    }
}
