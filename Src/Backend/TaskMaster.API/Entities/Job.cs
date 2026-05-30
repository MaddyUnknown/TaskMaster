using TaskMaster.Entities.Abstractions;
using TaskMaster.Enums;

namespace TaskMaster.Entities
{
    public class Job : BaseEntity
    {
        public Guid JobPublicId { get; set; }
        public string JobType { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public JobStatusEnum Status { get; set; }

        //Foreign key
        public long? AssignedWorkerId { get; set; }

    }
}
