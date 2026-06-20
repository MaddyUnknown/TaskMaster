using TaskMaster.Library.Common.Models.JobType;

namespace TaskMaster.Library.Common.Models.Jobs
{
    public class JobDetails
    {
        public Guid JobId { get; set; }
        public JobTypeRef JobType { get; set; } = JobTypeRef.Empty;
        public string? Payload { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
