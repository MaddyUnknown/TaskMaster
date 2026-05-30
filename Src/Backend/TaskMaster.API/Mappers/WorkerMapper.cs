using TaskMaster.Entities;
using TaskMaster.Models.Workers;
using TaskMaster.API.Enums;

namespace TaskMaster.Mappers
{
    public static class WorkerMapper
    {
        public static Worker ToWorker(this RegisterWorkerRequest request)
        {
            return new Worker
            {
                WorkerPublicId = Guid.NewGuid(),
                WorkerName = request.WorkerName,
                JobTypeCapabilities = request.JobTypeCapabilities.Select(c => new WorkerCapabality { JobType = c }).ToList(),
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
                JobTypeCapabilities = worker.JobTypeCapabilities.Select(w => w.JobType).ToArray()
            };
        }
        
        public static RegisterWorkerResponse ToRegisterWorkerResponse(this Worker worker, int heartBeatIntervalSeconds)
        {
            return new RegisterWorkerResponse
            {
                WorkerId = worker.WorkerPublicId,
                Status = worker.Status,
                HeartBeatIntervalSeconds = heartBeatIntervalSeconds
            };
        }
    }
}
