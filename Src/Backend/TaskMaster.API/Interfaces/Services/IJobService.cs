using TaskMaster.Enums;
using TaskMaster.Models.Jobs;
using TaskMaster.Models.Workers;

namespace TaskMaster.Interfaces.Services
{
    public interface IJobService
    {
        Task<JobDetails> CreateAsync(JobCreateRequest job);
        Task<JobDetails> ChangeJobStatusAsync(Guid jobId, JobStatusEnum status, WorkerIdRef workerIdRef);
        Task<JobDetails?> GetNextWorkerJobsAsync(Guid workerId);
    }
}
