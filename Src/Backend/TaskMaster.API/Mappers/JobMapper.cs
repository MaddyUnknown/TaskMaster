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
            var jobType = jobEntity.JobType;
            return new JobDetails
            {
                JobId = jobEntity.JobPublicId,
                JobType = (jobType == null) ? JobTypeRef.Empty : new JobTypeRef { Name = jobType.Name, Version = jobType.Version },
                Payload = jobEntity.Payload,
                Status = jobEntity.Status,
                CreatedDateTime = jobEntity.CreatedDateTime,
                ModifyDateTime = jobEntity.ModifyDateTime,
                CompletedDateTime = jobEntity.Status == JobStatusEnum.Completed ? jobEntity.ModifyDateTime : null,
                FailDateTime = jobEntity.Status == JobStatusEnum.Failed ? jobEntity.ModifyDateTime : null,
            };
        }
    }
}
