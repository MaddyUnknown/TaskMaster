using TaskMaster.API.Enums;
using TaskMaster.API.Models.JobTypes;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Models.Jobs
{
    public class JobDetails
    {
        public Guid JobId { get; set; }
        public JobTypeRef JobType { get; set; } = JobTypeRef.Empty;
        public string? Payload { get; set; } = string.Empty;
        public JobStatusEnum Status { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifyDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public DateTime? FailDateTime { get; set; }
    }
}
