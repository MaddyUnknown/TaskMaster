using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TaskMaster.Demo.Web.BackgroundServices;
using TaskMaster.Demo.Web.Configs;
using TaskMaster.Demo.Web.Data;
using TaskMaster.Demo.Web.Domain;
using TaskMaster.Demo.Web.Reports;
using TaskMaster.Library.Producer.Configs;
using TaskMaster.Library.Producer.DependencyInjection;

namespace TaskMaster.Demo.Web;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddUserSecrets<DemoDbContext>();

        var demo = builder.Configuration.GetSection(DemoOptions.SectionName).Get<DemoOptions>() ?? new DemoOptions();
        builder.Services.AddSingleton(demo);
        builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.SectionName));

        builder.Services.AddControllers();

        // ---- TaskMaster integration -------------------------------------------
        // Producer SDK: creates jobs via client credentials against the existing
        // TaskMaster API. Credentials are supplied through configuration/secrets.
        var apiBaseUrl = builder.Configuration["TaskMaster:ApiBaseUrl"];
        builder.Services.AddTaskMasterProducer(options =>
        {
            if (!string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                options.ApiBaseUrl = apiBaseUrl;
            }

            var clientId = builder.Configuration["TaskMaster:Oidc:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                options.Auth.Oidc = new TaskMasterProducerOidcAuthOptions
                {
                    ClientId = clientId,
                    ClientSecret = builder.Configuration["TaskMaster:Oidc:ClientSecret"] ?? string.Empty
                };
            }
        });

        // ---- Demo persistence --------------------------------------------------
        // The demo owns one table: the report row the UI polls. Job persistence and
        // lifecycle remain entirely TaskMaster's responsibility.
        builder.Services.AddDemoPersistence(builder.Configuration);
        builder.Services.AddScoped<IReportRepository, EfReportRepository>();
        builder.Services.AddDemoStorage(builder.Configuration);
        builder.Services.AddSingleton(new ReportRequestValidator(demo));

        // Expire generated reports and their files after the configured retention.
        builder.Services.AddHostedService<ReportRetentionService>();

        // ---- Rate limiting ---------------------------------------------------
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("submit", context =>
            {
                var partitionKey = $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, demo.MaxJobSubmissionsPerUserPerMinute),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        var app = builder.Build();

        await EnsureDemoSchemaAsync(app);

        // Surface unhandled failures as JSON without leaking internals.
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                app.Logger.LogError(ex, "Unhandled exception while handling {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Unexpected server error." });
            }
        });

        app.MapControllers();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRateLimiter();

        app.MapFallbackToFile("index.html");

        app.Run();
    }

    /// <summary>
    /// Creates the demo schema on startup when <c>Database:AutoMigrate</c> is enabled.
    ///
    /// This runs in Demo.Web only, and deliberately so: <c>EnsureCreated</c> is not safe
    /// to run from two processes at once, and Demo.Web is the single entry point to the
    /// demo. TaskMaster.Demo.Consumer assumes the table already exists — start this app
    /// once before the consumer, and set AutoMigrate to false when several replicas run
    /// and the schema is created out of band.
    /// </summary>
    private static async Task EnsureDemoSchemaAsync(WebApplication app)
    {
        var autoMigrate = app.Configuration.GetValue<bool?>(DemoPersistenceServiceCollectionExtensions.AutoMigrateConfigurationKey) ?? true;

        if (!autoMigrate)
        {
            app.Logger.LogInformation("Database:AutoMigrate is false; skipping demo schema creation.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoDbContext>();

        if (await context.Database.EnsureCreatedAsync())
        {
            app.Logger.LogInformation("Created the demo database schema (table: ReportRequests).");
        }
    }
}
