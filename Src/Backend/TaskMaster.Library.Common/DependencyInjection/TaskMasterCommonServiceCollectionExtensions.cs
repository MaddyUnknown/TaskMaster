using Microsoft.Extensions.Caching.Memory;
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
            services.Configure<MemoryCacheOptions>(options =>
            {
                options.SizeLimit = 200; // 2 MB assuming each schema record takes 10KB
                options.CompactionPercentage = 0.2; // 20% clear of cache when full
            });

            services.AddSingleton(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<MemoryCacheOptions>>();
                return new MemoryCache(opts);
            });

            services.AddSingleton<IMemoryCache>(sp => sp.GetRequiredService<MemoryCache>());
            services.AddSingleton<ICache, InMemoryCache>();
            services.AddSingleton<IApiHttpClient, ApiHttpClient>();
            services.AddSingleton<IJobTypeSchemaRegistry, JobTypeSchemaRegistry>();
        }
    }
}
