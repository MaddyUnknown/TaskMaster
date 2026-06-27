using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Models.Jobs
{
    public class CreateJob
    {
        public JobTypeRef JobType { get; set; } = JobTypeRef.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
