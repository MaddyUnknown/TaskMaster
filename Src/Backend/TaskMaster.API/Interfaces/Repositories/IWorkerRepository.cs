using TaskMaster.API.Entities;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IWorkerRepository
    {
        Task<Worker?> GetByPublicIdAsync(Guid workerPublicId);
        Task<Worker?> GetByWorkerNameAsync(string workerName, bool withLock = false);
        Task<Worker?> UpdateWorkerExpiryAndReturnAsync(Guid workerPublicId, int workerExpiryIntervalSeconds);
        Task<PagedResult<Worker>> GetAllWorkersAsync(WorkerQuery query);
        Task<int> DeactivateExpiredWorkersAsync();
        Task<List<Worker>> GetExpiredActiveWorkersAsync();
        Task<WorkerStatusCounts> CountWorkersByStatusAsync();
    }
}
