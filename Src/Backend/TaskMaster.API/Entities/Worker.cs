using TaskMaster.API.Enums;
using TaskMaster.Entities.Abstractions;

namespace TaskMaster.Entities
{
    public class Worker : BaseEntity
    {
        public Guid WorkerPublicId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public WorkerStatusEnum Status { get; set; }

        //Navigation Property
        public ICollection<Job> AssignedJobs { get; set; } = null!;
        public ICollection<WorkerCapabality> JobTypeCapabilities { get; set; } = null!;
    }
}
