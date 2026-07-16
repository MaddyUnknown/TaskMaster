using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IWorkerRepository
    {
        Task<Worker?> GetByPublicIdAsync(Guid workerPublicId);
        Task<Worker?> GetByWorkerNameAsync(string workerName, bool withLock = false);
        Task<Worker?> UpdateWorkerExpiryAndReturnAsync(Guid workerPublicId, int workerExpiryIntervalSeconds);
        Task<IEnumerable<Worker>> GetAllWorkersAsync();
        Task<int> DeactivateExpiredWorkersAsync();
    }
}
