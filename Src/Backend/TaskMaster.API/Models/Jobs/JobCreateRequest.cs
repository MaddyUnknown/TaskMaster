using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Enums;

namespace TaskMaster.API.Models.Jobs
{
    public class JobCreateRequest
    {
        public JobTypeDetails JobType { get; set; } = JobTypeDetails.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
