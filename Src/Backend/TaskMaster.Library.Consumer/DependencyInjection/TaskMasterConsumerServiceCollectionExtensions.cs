using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.DependencyInjection;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Factories;
using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Library.Consumer.DependencyInjection
{
    public static class TaskMasterConsumerServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskMasterConsumer(this IServiceCollection services, Action<TaskMasterConsumerOptions> configureOptions)
        {
            var options = new TaskMasterConsumerOptions();
            configureOptions(options);

            services.AddTaskMasterCommon(new AppConfig
            {
                ApiBaseUrl = options.ApiBaseUrl,
                Auth = new AuthConfig
                {
                    Oidc = options.Auth.Oidc == null ? null : new OidcConfig
                    {
                        ClientId = options.Auth.Oidc.ClientId,
                        ClientSecret = options.Auth.Oidc.ClientSecret,
                        RequestedScope = "jobs:pull jobs:report workers:register workers:heartbeat workers:remove jobtypes:read"
                    }
                }
            });

            services.AddSingleton(Options.Create(options));
            services.AddTransient<IWorkerFactory, TaskWorkerFactory>();

            return services;
        }
    }
}
