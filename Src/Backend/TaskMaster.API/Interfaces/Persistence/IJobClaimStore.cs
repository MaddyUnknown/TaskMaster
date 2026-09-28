using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Persistence
{
    public interface IJobClaimStore
    {
        Task<List<Job>> ClaimJobsAsync(Guid workerPublicId, int maxJobs, CancellationToken cancellationToken = default);

        Task<int> UnassignJobsForInactiveWorkersAsync(CancellationToken cancellationToken = default);

        Task<int> UnassignJobsForWorkerIdAsync(long workerId, CancellationToken cancellationToken = default);
    }
}
