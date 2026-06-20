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
            services.AddTaskMasterProducerCore(configure);

            return services;
        }

        internal static IServiceCollection AddTaskMasterProducerCore(this IServiceCollection services, Action<TaskMasterProducerOptions> configure)
        {
            var options = new TaskMasterProducerOptions();
            configure(options);

            services.AddTaskMasterCommon(new ApiConfig
            {
                ApiBaseUrl = options.ApiBaseUrl
            });

            services.AddTransient<IProducer, TaskProducer>();

            return services;
        }
    }
}
