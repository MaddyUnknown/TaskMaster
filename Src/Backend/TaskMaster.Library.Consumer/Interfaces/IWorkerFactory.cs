using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Library.Consumer.Interfaces
{
    public interface IWorkerFactory
    {
        IWorker CreateWorker(string workerName, Action<WorkerConfiguration> configure);
    }
}
