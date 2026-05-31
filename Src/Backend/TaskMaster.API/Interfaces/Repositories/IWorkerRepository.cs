using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IWorkerRepository
    {
        Task<Worker?> GetByPublicIdAsync(Guid workerPublicId);
        Task<int> UpdateWorkerExpiryTimestampAsync(Guid workerPublicId, int workerExpiryIntervalSeconds);
    }
}
