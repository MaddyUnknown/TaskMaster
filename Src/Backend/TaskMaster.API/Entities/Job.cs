using System.ComponentModel.DataAnnotations;
using TaskMaster.API.Entities;
using TaskMaster.API.Entities.Abstractions;
using TaskMaster.API.Enums;

namespace TaskMaster.API.Entities
{
    public class Job : BaseEntity
    {
        [Required]
        public Guid JobPublicId { get; set; }

        [Required]
        public string? Payload { get; set; }

        [Required]
        public JobStatusEnum Status { get; set; }

        //Foreign key
        public long? AssignedWorkerId { get; set; }

        [Required]
        public long JobTypeId { get; set; }

        //Navigation property
        public JobType JobType { get; set; } = null!;
        public Worker? AssignedWorker { get; set; } = null;

    }
}
