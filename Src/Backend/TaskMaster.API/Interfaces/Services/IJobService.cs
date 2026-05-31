using TaskMaster.API.Enums;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Interfaces.Services
{
    public interface IJobService
    {
        Task<JobDetails> CreateAsync(CreateJob job);
        Task<JobDetails> ChangeJobStatusAsync(Guid jobId, JobStatusEnum status, WorkerIdRef workerIdRef);
        Task<JobDetails?> GetNextWorkerJobsAsync(Guid workerId);
    }
}
