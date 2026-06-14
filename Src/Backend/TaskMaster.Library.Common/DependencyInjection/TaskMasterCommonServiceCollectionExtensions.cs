using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskMaster.Library.Common.Caches;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.HttpClients;
using TaskMaster.Library.Common.Interfaces.Caches;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Registries;

namespace TaskMaster.Library.Common.DependencyInjection
{
    public static class TaskMasterCommonServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskMasterCommon(this IServiceCollection services, Action<ApiConfig> configure)
        {
            services.Configure(configure);
            RegisterCommonService(services);
            return services;
        }

        public static IServiceCollection AddTaskMasterCommon(this IServiceCollection services, ApiConfig config)
        {
            services.AddSingleton(Options.Create(config));
            RegisterCommonService(services);
            return services;
        }

        private static void RegisterCommonService(IServiceCollection services)
        {
            services.AddSingleton<ICache>(_ => new InMemoryCache());
            services.AddSingleton<IApiHttpClient>(serviceProvider => new ApiHttpClient(serviceProvider.GetRequiredService<IOptions<ApiConfig>>()));
            services.AddSingleton<IJobTypeSchemaRegistry>(serviceProvider => new JobTypeSchemaRegistry(
                serviceProvider.GetRequiredService<IApiHttpClient>(),
                serviceProvider.GetRequiredService<ICache>()));
        }
    }
}
