using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Interfaces.Services
{
    public interface IWorkerService
    {
        Task<RegisterWorkerResponse> RegisterAsync(RegisterWorkerRequest registerWorker);
        Task<WorkerDetails> RemoveAsync(Guid workerId);
        Task<ActionStatusResponse> HeartBeatAsync(Guid workerId);
    }
}
