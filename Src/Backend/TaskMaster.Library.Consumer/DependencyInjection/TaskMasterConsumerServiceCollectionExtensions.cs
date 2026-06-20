using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.DependencyInjection;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Interfaces;
using TaskMaster.Library.Consumer.Consumers;

namespace TaskMaster.Library.Consumer.DependencyInjection
{
    public static class TaskMasterConsumerServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskMasterConsumer(this IServiceCollection services, Action<TaskMasterConsumerOptions> configure)
        {
            services.AddTaskMasterConsumerCore(configure);

            return services;
        }

        internal static IServiceCollection AddTaskMasterConsumerCore(this IServiceCollection services, Action<TaskMasterConsumerOptions> configure)
        {
            var options = new TaskMasterConsumerOptions();
            configure(options);

            services.AddTaskMasterCommon(new ApiConfig
            {
                ApiBaseUrl = options.ApiBaseUrl
            });

            services.AddSingleton(Options.Create(options));
            services.AddTransient(typeof(IWorker<>), typeof(TaskWorker<>));

            return services;
        }
    }
}
