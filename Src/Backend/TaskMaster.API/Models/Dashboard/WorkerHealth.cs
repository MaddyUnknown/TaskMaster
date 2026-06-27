using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Models.Dashboard
{
    public class WorkerHealth
    {
        public HealthStatus Status { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
    }
}
