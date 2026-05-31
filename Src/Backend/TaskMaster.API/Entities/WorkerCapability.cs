using System.ComponentModel.DataAnnotations;
using TaskMaster.API.Entities;
using TaskMaster.API.Entities.Abstractions;

namespace TaskMaster.API.Entities
{
    public class WorkerCapability : BaseEntity
    {
        // Foreign Key
        [Required]
        public long WorkerId { get; set; }

        [Required]
        public long JobTypeId { get; set; }

        // Navigation property
        public JobType JobType { get; set; } = null!;
    }
}
