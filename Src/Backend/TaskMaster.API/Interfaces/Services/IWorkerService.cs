using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Interfaces.Services
{
    public interface IWorkerService
    {
        Task<RegisterWorkerResponse> RegisterAsync(RegisterWorker registerWorker);
        Task<WorkerDetails> RemoveAsync(Guid workerId);
        Task<HeartbeatActionStatus> HeartBeatAsync(Guid workerId);
        Task<IEnumerable<WorkerDetails>> GetAllWorkersAsync();
        Task<WorkerDetails?> GetWorkerByPublicIdAsync(Guid workerId);
    }
}
