using TaskMaster.API.Enums;
using TaskMaster.API.Models.Jobs;

namespace TaskMaster.API.Models.Workers
{
    public class WorkerDetails
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public WorkerStatusEnum Status { get; set; }
        public IEnumerable<JobTypeDetails> JobTypeCapabilities { get; set; } = Enumerable.Empty<JobTypeDetails>();
    }
}
