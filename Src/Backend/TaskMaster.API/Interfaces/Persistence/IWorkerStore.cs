using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Persistence
{
    public interface IWorkerStore
    {
        Task<Worker?> GetByWorkerNameAsync(string workerName, bool withLock = false, CancellationToken cancellationToken = default);

        Task<Worker?> UpdateWorkerExpiryAndReturnAsync(
            Guid workerPublicId,
            int workerExpiryIntervalSeconds,
            CancellationToken cancellationToken = default);

        Task<int> DeactivateExpiredWorkersAsync(CancellationToken cancellationToken = default);
    }
}
