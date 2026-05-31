using System.ComponentModel.DataAnnotations;
using TaskMaster.API.Entities.Abstractions;

namespace TaskMaster.API.Entities
{
    public class JobType : BaseEntity
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public long Version { get; set; }

        [Required]
        public string Schema { get; set; } = string.Empty;
    }
}
