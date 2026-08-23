using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.DependencyInjection;
using TaskMaster.Library.Producer.Configs;
using TaskMaster.Library.Producer.Interfaces;
using TaskMaster.Library.Producer.Producers;

namespace TaskMaster.Library.Producer.DependencyInjection
{
    public static class TaskMasterProducerServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskMasterProducer(this IServiceCollection services, Action<TaskMasterProducerOptions> configure)
        {
            var options = new TaskMasterProducerOptions();
            configure(options);

            services.AddTaskMasterCommon(new AppConfig
            {
                ApiBaseUrl = options.ApiBaseUrl,
                Auth = new AuthConfig
                {
                    Oidc = options.Auth.Oidc == null ? null : new OidcConfig
                    {
                        ClientId = options.Auth.Oidc.ClientId,
                        ClientSecret = options.Auth.Oidc.ClientSecret,
                        RequestedScope = "jobs:create jobtypes:read"
                    }
                }
            });

            services.AddTransient<IProducer, TaskProducer>();

            return services;
        }
    }
}
