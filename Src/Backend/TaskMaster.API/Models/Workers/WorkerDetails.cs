using TaskMaster.API.Enums;

namespace TaskMaster.Models.Workers
{
    public class WorkerDetails
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public WorkerStatusEnum Status { get; set; }
        public string[] JobTypeCapabilities { get; set; } = Array.Empty<string>();
    }
}
