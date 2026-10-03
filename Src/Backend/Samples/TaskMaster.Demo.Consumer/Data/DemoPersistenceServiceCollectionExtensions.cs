using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskMaster.Demo.Consumer.Data;

public static class DemoPersistenceServiceCollectionExtensions
{
    public const string DefaultConnectionStringName = "DefaultConnection";
    public const string ProviderConfigurationKey = "Database:Provider";

    public const string SqlServerProvider = "SqlServer";
    public const string PostgreSqlProvider = "PostgreSql";

    public static IServiceCollection AddDemoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = ResolveProvider(configuration);
        var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionStringName}' was not configured. " +
                "It must be the same database Demo.Web uses.");
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
