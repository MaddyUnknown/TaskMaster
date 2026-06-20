using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Workers;
using TaskMaster.Library.Consumer.Interfaces;
using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Library.Consumer.Factories
{
    public class TaskWorkerFactory : IWorkerFactory
    {
        private IServiceProvider _serviceProvider;
        private IOptions<TaskMasterConsumerOptions> _options;

        public TaskWorkerFactory()
        {
            var sp = TaskMasterConsumer.ServiceProducer;
            _serviceProvider = sp;
            _options = sp.GetRequiredService<IOptions<TaskMasterConsumerOptions>>();
        }

        public TaskWorkerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _options = serviceProvider.GetRequiredService<IOptions<TaskMasterConsumerOptions>>();
        }

        public IWorker CreateWorker(string workerName, Action<WorkerConfiguration> configure)
        {
            var config = new WorkerConfiguration();
            configure(config);
            return (IWorker) ActivatorUtilities.CreateInstance(_serviceProvider, typeof(TaskWorker), workerName, config, _options);
        }
    }
}
