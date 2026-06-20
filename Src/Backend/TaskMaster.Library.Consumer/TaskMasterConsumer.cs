using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Consumer.Constants;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.DependencyInjection;

namespace TaskMaster.Library.Consumer
{
    public sealed class TaskMasterConsumer : IDisposable
    {
        private static TaskMasterConsumer? _instance;
        internal static ServiceProvider ServiceProducer => _instance?._serviceProvider ?? throw new Exception(ErrorMessage.ConsumerServiceNotInitialised());

        private ServiceProvider? _serviceProvider;

        public static TaskMasterConsumer Initialise(Action<TaskMasterConsumerOptions> configure)
        {
            if (_instance != null) throw new Exception(ErrorMessage.ConsumerServiceAlreadyInitialised());

            _instance = new TaskMasterConsumer(configure);
            return _instance;
        }

        private TaskMasterConsumer(Action<TaskMasterConsumerOptions> configure)
        {
            var services = new ServiceCollection();
            services.AddTaskMasterConsumer(configure);

            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;

            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }
    }
}
