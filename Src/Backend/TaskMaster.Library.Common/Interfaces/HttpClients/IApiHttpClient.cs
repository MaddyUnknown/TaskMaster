using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Common.Models.Workers;

namespace TaskMaster.Library.Common.Interfaces.HttpClients
{
    public interface IApiHttpClient
    {
        Task<JobTypeDetails?> GetJobType(GetJobTypeRequest request);
        Task<JobDetails> CreateJob(CreateJobRequest request);
        Task<RegisterWorkerResponse> RegisterWorker(RegisterWorker worker);
        Task<WorkerDetails> RemoveWorker(Guid workerId);
        Task<HeartbeatActionStatus> WorkerHeartBeat(Guid workerId);
        Task<JobDetails?> PullJob(Guid workerId);
        Task<JobDetails> CompleteJob(Guid jobId, Guid workerId);
        Task<JobDetails> FailJob(Guid jobId, Guid workerId);
    }
}
