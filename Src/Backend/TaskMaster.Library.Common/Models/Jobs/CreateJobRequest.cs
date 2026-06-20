using TaskMaster.Library.Common.Models.JobType;

namespace TaskMaster.Library.Common.Models.Jobs
{
    public class CreateJobRequest
    {
        public JobTypeRef JobType { get; set; } = new();
        public string Payload { get; set; } = string.Empty;
    }
}
