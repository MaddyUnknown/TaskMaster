using TaskMaster.API.Entities.Abstractions;
using TaskMaster.API.Enums;

namespace TaskMaster.API.Entities
{
    public class SystemActivity : BaseEntity
    {
        public EntityType EntityType { get; set; }

        public Guid EntityId { get; set; }

        public ActivityType ActivityType { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
