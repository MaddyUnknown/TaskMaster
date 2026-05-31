using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Mappers
{
    public static class JobTypeMapper
    {
        public static JobType ToJobType(this CreateJobType createJobTypeRequest)
        {
            return new JobType
            {
                Name = createJobTypeRequest.Name,
                Version = createJobTypeRequest.Version,
                Schema = createJobTypeRequest.Schema
            };
        }

        public static JobTypeDetails ToJobTypeDetails(this JobType jobType)
        {
            return new JobTypeDetails
            {
                Name = jobType.Name,
                Version = jobType.Version,
                Schema = jobType.Schema
            };
        }
    }
}
