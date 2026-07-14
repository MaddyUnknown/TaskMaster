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

        [Required]
        public DateTime LastHeartBeatTimestamp { get; set; }

        [Required]
        public DateTime WorkerExpiresAtTimestamp { get; set; }

        //Navigation Property
        public ICollection<WorkerCapability> WorkerCapabilities { get; set; } = null!;
    }
}
