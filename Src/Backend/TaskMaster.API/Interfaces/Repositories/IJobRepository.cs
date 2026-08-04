using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IJobRepository
    {
        Task<Job?> GetByJobPublicIdAsync(Guid jobPublicId);
        Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId);
        Task<int> UnassignJobForWorkerIdAsync(long workerId);
        Task<int> UnassignJobsForInactiveWorkersAsync();
        Task<List<Job>> GetNextJobsForWorkerAsync(Guid workerPublicId, int maxJobs);
        Task<IEnumerable<Job>> GetAllJobsAsync();
    }
}
