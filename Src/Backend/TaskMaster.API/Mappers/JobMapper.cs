using TaskMaster.Entities;
using TaskMaster.Enums;
using TaskMaster.Models.Jobs;

namespace TaskMaster.Mappers
{
    public static class JobMapper
    {
        public static Job ToJob(this JobCreateRequest jobCreateRequest)
        {
            return new Job
            {
                JobPublicId = Guid.NewGuid(),
                JobType = jobCreateRequest.JobType,
                Payload = jobCreateRequest.Payload,
                Status = JobStatusEnum.Queued,
            };
        }

        public static JobDetails ToJobDetails(this Job jobEntity)
        {
            return new JobDetails
            {
                JobId = jobEntity.JobPublicId,
                JobType = jobEntity.JobType,
                Payload = jobEntity.Payload,
                Status = jobEntity.Status
            };
        }
    }
}
