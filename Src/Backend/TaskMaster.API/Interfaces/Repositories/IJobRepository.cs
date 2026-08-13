using TaskMaster.API.Entities;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IJobRepository
    {
        Task<Job?> GetByJobPublicIdAsync(Guid jobPublicId);
        Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId);
        Task<int> UnassignJobForWorkerIdAsync(long workerId);
        Task<int> UnassignJobsForInactiveWorkersAsync();
        Task<List<Job>> GetNextJobsForWorkerAsync(Guid workerPublicId, int maxJobs);
        Task<PagedResult<Job>> GetAllJobsAsync(JobQuery query);
        Task<JobStatusCounts> CountJobsByStatusAsync();
    }
}
