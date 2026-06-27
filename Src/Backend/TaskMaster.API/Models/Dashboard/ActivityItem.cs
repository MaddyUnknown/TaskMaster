using TaskMaster.API.Enums;
using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Models.Dashboard
{
    public class ActivityItem
    {
        public JobStatusEnum Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public ActivityStatus Status { get; set; }
    }
}
