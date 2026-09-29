using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Demo.Web.Data;

public static class DemoPersistenceServiceCollectionExtensions
{
    public const string DefaultConnectionStringName = "DefaultConnection";
    public const string ProviderConfigurationKey = "Database:Provider";
    public const string AutoMigrateConfigurationKey = "Database:AutoMigrate";

    public const string SqlServerProvider = "SqlServer";
    public const string PostgreSqlProvider = "PostgreSql";

    /// <summary>
    /// Registers the demo context for the configured engine. Provider-agnostic on
    /// purpose: every query in <c>EfReportRepository</c> is plain LINQ, so the same
    /// code path runs on SQL Server and PostgreSQL.
    /// </summary>
    public static IServiceCollection AddDemoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = ResolveProvider(configuration);
        var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionStringName}' was not configured. " +
                "Set it with: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\"");
        }

        switch (provider)
        {
            case SqlServerProvider:
                services.AddDbContext<DemoDbContext>(options => options.UseSqlServer(connectionString));
                break;

            case PostgreSqlProvider:
                services.AddDbContext<DemoDbContext>(options => options.UseNpgsql(connectionString));
                break;

            default:
                throw new InvalidOperationException(
                    $"'{ProviderConfigurationKey}' was '{provider}'. Valid values are: {SqlServerProvider}, {PostgreSqlProvider}.");
        }

        return services;
    }

    internal static string ResolveProvider(IConfiguration configuration)
    {
        var configured = configuration[ProviderConfigurationKey];

        return string.IsNullOrWhiteSpace(configured) ? SqlServerProvider : configured.Trim();
    }
}
