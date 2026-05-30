using TaskMaster.Entities;

namespace TaskMaster.Interfaces.Repositories
{
    public interface IWorkerRepository
    {
        Task<Worker?> GetByPublicIdAsync(Guid workerPublicId);
        Task<Worker?> UpdateWorkerExpiryTimestampAsync(Guid workerPublicId, int workerExpiryIntervalSeconds);
    }
}
