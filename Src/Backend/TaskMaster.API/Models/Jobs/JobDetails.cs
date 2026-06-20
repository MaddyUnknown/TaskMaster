using TaskMaster.API.Enums;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Models.Jobs
{
    public class JobDetails
    {
        public Guid JobId { get; set; }
        public JobTypeRef JobType { get; set; } = JobTypeRef.Empty;
        public string? Payload { get; set; } = string.Empty;
        public JobStatusEnum Status { get; set; }
    }
}
