using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Producer.Constants;
using TaskMaster.Library.Producer.Configs;
using TaskMaster.Library.Producer.DependencyInjection;

namespace TaskMaster.Library.Producer
{
    public sealed class TaskMasterProducer: IDisposable
    {
        // Static
        private static TaskMasterProducer? _instance;
        internal static ServiceProvider ServiceProducer => _instance?._serviceProvider ?? throw new InvalidOperationException(ErrorMessage.ProducerServiceNotInitialised());

        // Instance
        private ServiceProvider? _serviceProvider;

        public static TaskMasterProducer Initialise(Action<TaskMasterProducerOptions> configure)
        {
            if (_instance != null) throw new InvalidOperationException(ErrorMessage.ProducerServiceAlreadyInitialised());

            _instance = new TaskMasterProducer(configure);
            return _instance;
        }

        private TaskMasterProducer(Action<TaskMasterProducerOptions> configure)
        {
            var services = new ServiceCollection();
            services.AddTaskMasterProducer(configure);

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
