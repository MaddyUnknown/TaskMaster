using TaskMaster.Enums;

namespace TaskMaster.Models.Jobs
{
    public class JobCreateRequest
    {
        public string JobType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
