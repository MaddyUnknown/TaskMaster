using TaskMaster.API.Enums;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Models.Workers
{
    public class WorkerDetails
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string? WorkerDisplayName { get; set; } = string.Empty;
        public WorkerStatusEnum Status { get; set; }
        public DateTime WorkerExpiresAtTimestamp { get; set; }
        public DateTime LastHeartBeatTimestamp { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public IEnumerable<JobTypeRef> JobTypeCapabilities { get; set; } = Enumerable.Empty<JobTypeRef>();

        public static WorkerDetails Empty => new WorkerDetails();
    }
}
