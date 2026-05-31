using TaskMaster.API.Enums;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Models.Jobs
{
    public class CreateJob
    {
        public GetJobType JobType { get; set; } = GetJobType.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
