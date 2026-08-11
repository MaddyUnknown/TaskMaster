using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Interfaces.Services
{
    public interface IWorkerService
    {
        Task<RegisterWorkerResponse> RegisterAsync(RegisterWorker registerWorker);
        Task<WorkerDetails> RemoveAsync(Guid workerId);
        Task<HeartbeatActionStatus> HeartBeatAsync(Guid workerId);
        Task<PagedResult<WorkerDetails>> GetAllWorkersAsync(WorkerQuery query);
        Task<WorkerDetails?> GetWorkerByPublicIdAsync(Guid workerId);
        Task<WorkerStatusCounts> GetWorkerStatusCountsAsync();
    }
}
