using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Library.Consumer.Interfaces
{
    public interface IWorker<T>
    {
        Task<JobConsumeResult<T>?> ConsumeAsync();
        Task CompleteAsync(JobConsumeResult<T> jobResult);
        Task FailAsync(JobConsumeResult<T> jobResult);
    }
}
