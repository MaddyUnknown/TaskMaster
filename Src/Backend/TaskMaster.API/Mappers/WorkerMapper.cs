using TaskMaster.API.Entities;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Enums;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Mappers
{
    public static class WorkerMapper
    {
        public static Worker ToWorker(this RegisterWorker request, IEnumerable<JobType> jobTypes)
        {
            return new Worker
            {
                WorkerPublicId = Guid.NewGuid(),
                WorkerName = request.WorkerName,
                WorkerCapabilities = jobTypes.Select(t => new WorkerCapability { JobType = t }).ToList(),
                Status = WorkerStatusEnum.Active,
            };
        }

        public static WorkerDetails ToWorkerDetails(this Worker worker)
        {
            return new WorkerDetails
            {
                WorkerId = worker.WorkerPublicId,
                WorkerName = worker.WorkerName,
                Status = worker.Status,
                JobTypeCapabilities = worker.WorkerCapabilities.Where(w => w?.JobType != null).Select(w => new JobTypeRef { Name = w.JobType.Name, Version = w.JobType.Version }).ToArray()
            };
        }
        
        public static RegisterWorkerResponse ToRegisterWorkerResponse(this Worker worker, int heartBeatIntervalSeconds)
        {
            return new RegisterWorkerResponse
            {
                WorkerDetails = worker.ToWorkerDetails(),
                HeartBeatIntervalSeconds = heartBeatIntervalSeconds
            };
        }
    }
}
