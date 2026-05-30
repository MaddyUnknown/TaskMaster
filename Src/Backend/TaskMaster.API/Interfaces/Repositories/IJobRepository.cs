using TaskMaster.Entities;

namespace TaskMaster.Interfaces.Repositories
{
    public interface IJobRepository
    {
        Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId);
        Task<int> UnassignJobForWorkerId(long workerId);
        Task<Job?> GetNextJobForWorkerAsync(long workerId);
    }
}
