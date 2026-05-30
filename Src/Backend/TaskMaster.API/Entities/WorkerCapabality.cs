using TaskMaster.Entities.Abstractions;

namespace TaskMaster.Entities
{
    public class WorkerCapabality : BaseEntity
    {
        public string JobType { get; set; } = string.Empty;

        // Foreign Key
        public long WorkerId { get; set; }
    }
}
