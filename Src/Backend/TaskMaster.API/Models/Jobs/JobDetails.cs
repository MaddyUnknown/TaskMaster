using TaskMaster.API.Enums;

namespace TaskMaster.API.Models.Jobs
{
    public class JobDetails
    {
        public Guid JobId { get; set; }
        public JobTypeDetails JobType { get; set; } = JobTypeDetails.Empty;
        public string? Payload { get; set; } = string.Empty;
        public JobStatusEnum Status { get; set; }
    }
}
