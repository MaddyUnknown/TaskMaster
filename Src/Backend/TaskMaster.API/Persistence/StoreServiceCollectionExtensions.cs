using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Persistence.PostgreSql;
using TaskMaster.API.Persistence.SqlServer;

namespace TaskMaster.API.Persistence
{
    public static class StoreServiceCollectionExtensions
    {
        /// <summary>
        /// Binds each dialect agnostic store contract to the implementation for the selected
        /// engine. Registered together with the context so the context and the SQL dialect can
        /// never end up mismatched.
        /// </summary>
        public static IServiceCollection AddTaskMasterPersistenceStores(
            this IServiceCollection services,
            DatabaseProviderEnum provider)
        {
            switch (provider)
            {
                case DatabaseProviderEnum.PostgreSql:
                    services.AddScoped<IJobClaimStore, PostgreSqlJobClaimStore>();
                    services.AddScoped<IWorkerStore, PostgreSqlWorkerStore>();
                    services.AddScoped<IDashboardStatsStore, PostgreSqlDashboardStatsStore>();
                    break;

                case DatabaseProviderEnum.SqlServer:
                default:
                    services.AddScoped<IJobClaimStore, SqlServerJobClaimStore>();
                    services.AddScoped<IWorkerStore, SqlServerWorkerStore>();
                    services.AddScoped<IDashboardStatsStore, SqlServerDashboardStatsStore>();
                    break;
            }

            return services;
        }
    }
}
