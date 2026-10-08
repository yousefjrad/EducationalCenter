using EducationalCenter.API.Extensions;
using EducationalCenter.Application;
using EducationalCenter.Infrastructure;
using EducationalCenter.Infrastructure.Persistence;
using Asp.Versioning.ApiExplorer;
using Serilog;
using Serilog.Events;

// Logs problems that happen before the real logger exists (for example a missing setting).
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, logger) => logger
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        // One file per day, kept for 30 days: logs/educational-center-20261005.log
        .WriteTo.File(
            Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "educational-center-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            shared: true));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApi(builder.Configuration);

    var app = builder.Build();

    // Applies migrations and adds what is missing: permissions, roles, settings, templates, first Admin.
    await DatabaseInitializer.InitializeAsync(app.Services);

    // HTTPS is opt-in: set "Security:UseHttps": true where the site is served over HTTPS.
    if (app.Configuration.GetValue<bool>("Security:UseHttps"))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next();
    });

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        var versions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in versions.ApiVersionDescriptions)
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        });
    }

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "The application failed to start.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Lets integration tests start the application.
public partial class Program;
