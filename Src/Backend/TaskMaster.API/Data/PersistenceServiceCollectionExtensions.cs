using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Configs;
using TaskMaster.API.Data.SqlServer;
using TaskMaster.API.Data.PostgreSql;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Persistence;

namespace TaskMaster.API.Data
{
    public static class PersistenceServiceCollectionExtensions
    {
        public const string DefaultConnectionStringName = "DefaultConnection";

        public static IServiceCollection AddTaskMasterPersistence(this IServiceCollection services,IConfiguration configuration)
        {
            var provider = ResolveProvider(configuration);
            var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{DefaultConnectionStringName}' was not configured.");
            }

            switch (provider)
            {
                case DatabaseProviderEnum.SqlServer:
                    services.AddDbContext<SqlServerDbContext>(options => options.UseSqlServer(connectionString));
                    services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SqlServerDbContext>());
                    break;

                case DatabaseProviderEnum.PostgreSql:
                    services.AddDbContext<NpgsqlDbContext>(options => options.UseNpgsql(connectionString));
                    services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NpgsqlDbContext>());
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
            }

            services.AddTaskMasterPersistenceStores(provider);

            return services;
        }

        internal static DatabaseProviderEnum ResolveProvider(IConfiguration configuration)
        {
            var configured = configuration.GetSection(PersistenceConfig.SectionName)[nameof(PersistenceConfig.Provider)];

            if (string.IsNullOrWhiteSpace(configured))
            {
                return DatabaseProviderEnum.SqlServer;
            }

            if (!Enum.TryParse<DatabaseProviderEnum>(configured, ignoreCase: true, out var provider))
            {
                throw new InvalidOperationException($"'{PersistenceConfig.SectionName}:Provider' was '{configured}'. Valid values are: {string.Join(", ", Enum.GetNames<DatabaseProviderEnum>())}.");
            }

            return provider;
        }
    }
}
