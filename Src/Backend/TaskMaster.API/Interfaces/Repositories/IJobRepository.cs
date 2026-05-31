using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IJobRepository
    {
        Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId);
        Task<int> UnassignJobForWorkerIdAsync(long workerId);
        Task<Job?> GetNextJobForWorkerAsync(long workerId);
    }
}
