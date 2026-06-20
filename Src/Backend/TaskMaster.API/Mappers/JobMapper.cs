using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Mappers
{
    public static class JobMapper
    {
        public static Job ToJob(this CreateJob jobCreateRequest, JobType jobType)
        {
            return new Job
            {
                JobPublicId = Guid.NewGuid(),
                JobType = jobType,
                Payload = jobCreateRequest.Payload,
                Status = JobStatusEnum.Queued,
            };
        }

        public static JobDetails ToJobDetails(this Job jobEntity)
        {
            return new JobDetails
            {
                JobId = jobEntity.JobPublicId,
                JobType = (jobEntity.JobType == null) ? JobTypeRef.Empty : new JobTypeRef { Name = jobEntity.JobType.Name, Version = jobEntity.JobType.Version },
                Payload = jobEntity.Payload,
                Status = jobEntity.Status
            };
        }
    }
}
