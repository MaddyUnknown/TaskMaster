using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Models.Dashboard
{
    public class QueueHealth
    {
        public HealthStatus Status { get; set; }
        public int Depth { get; set; }
        public int Throughput { get; set; }
    }
}
