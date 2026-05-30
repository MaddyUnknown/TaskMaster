using TaskMaster.Enums;

namespace TaskMaster.Models.Jobs
{
    public class JobDetails
    {
        public Guid JobId { get; set; }
        public string JobType { get; set; } = string.Empty;
        public string? Payload { get; set; } = string.Empty;
        public JobStatusEnum Status { get; set; }
    }
}
