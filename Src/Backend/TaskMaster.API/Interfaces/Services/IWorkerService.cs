using TaskMaster.Models.Workers;

namespace TaskMaster.Interfaces.Services
{
    public interface IWorkerService
    {
        Task<RegisterWorkerResponse> RegisterAsync(RegisterWorkerRequest registerWorker);
        Task<WorkerDetails> RemoveAsync(Guid workerId);
        Task<ActionStatusResponse> HeartBeatAsync(Guid workerId);
    }
}
