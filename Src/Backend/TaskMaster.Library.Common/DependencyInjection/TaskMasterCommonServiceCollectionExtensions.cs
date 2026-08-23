using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TaskMaster.Library.Common.Auth;
using TaskMaster.Library.Common.Caches;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.HttpClients;
using TaskMaster.Library.Common.Interfaces.Caches;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.Auth;
using TaskMaster.Library.Common.Registries;

namespace TaskMaster.Library.Common.DependencyInjection
{
    public static class TaskMasterCommonServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskMasterCommon(this IServiceCollection services, Action<AppConfig> configure)
        {
            var config = new AppConfig();
            configure(config);

            services.AddSingleton(Options.Create(config));
            RegisterCommonService(services, config);
            return services;
        }

        public static IServiceCollection AddTaskMasterCommon(this IServiceCollection services, AppConfig config)
        {
            services.AddSingleton(Options.Create(config));
            RegisterCommonService(services, config);
            return services;
        }

        private static void RegisterCommonService(IServiceCollection services, AppConfig config)
        {
            // Common service
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
            services.AddSingleton<IJobTypeSchemaRegistry, JobTypeSchemaRegistry>();


            // Setup Http Pipeline
            services.AddHttpClient("public");
            var protectedHttpBuilder = services.AddHttpClient("protected");

            //services.AddSingleton<IApiHttpClient, ApiHttpClient>();
            services.TryAdd(ServiceDescriptor.Describe(typeof(IApiHttpClient), typeof(ApiHttpClient), ServiceLifetime.Singleton)); // For unit testing without server call

            // Get auth config
            var authConfig = GetAuthConfig(services).GetAwaiter().GetResult();
            if (authConfig == null) throw new InvalidOperationException(ErrorMessage.AuthApiConfigUnavailable());
            services.AddSingleton(Options.Create(authConfig));

            ValidateAuthConfig(config, authConfig);


            // Register auth specific dependencies and pipeline
            var authDependency = AuthDependencyFactory.GetAuthDependency(authConfig.Mode);
            authDependency.AddAuthServices(services);
            authDependency.AddAuthHttpClientHandler(protectedHttpBuilder);

        }

        private async static Task<AuthConfigDetails?> GetAuthConfig(IServiceCollection serviceCollection)
        {
            try
            {
                var serviceProvider = serviceCollection.BuildServiceProvider();
                var apiHttpClient = serviceProvider.GetRequiredService<IApiHttpClient>();

                return await apiHttpClient.GetAuthConfig();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ErrorMessage.AuthConfigFetchError(), ex);
            }
        }

        private static void ValidateAuthConfig(AppConfig config, AuthConfigDetails authConfig)
        {
            if(authConfig.Mode == EnumConstants.AuthModeEnum.Oidc)
            {
                if(config.Auth.Oidc == null)
                {
                    throw new InvalidOperationException(ErrorMessage.AuthModeNotConfigured(authConfig.Mode));
                }

                if(string.IsNullOrWhiteSpace(authConfig.Oidc?.Authority))
                {
                    throw new InvalidOperationException(ErrorMessage.AuthParameterNotConfigured(nameof(authConfig.Oidc.Authority), authConfig.Mode));
                }
            }
        }
    }
}
