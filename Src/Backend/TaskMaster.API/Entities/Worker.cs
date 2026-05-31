using TaskMaster.API.Enums;
using TaskMaster.API.Entities.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace TaskMaster.API.Entities
{
    public class Worker : BaseEntity
    {
        [Required]
        public Guid WorkerPublicId { get; set; }

        [Required]
        public string WorkerName { get; set; } = string.Empty;

        [Required]
        public WorkerStatusEnum Status { get; set; }

        //Navigation Property
        public ICollection<Job> AssignedJobs { get; set; } = null!;
        public ICollection<WorkerCapability> WorkerCapabilities { get; set; } = null!;
    }
}
