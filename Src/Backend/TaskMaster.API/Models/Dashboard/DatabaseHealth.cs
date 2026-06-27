using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Models.Dashboard
{
    public class DatabaseHealth
    {
        public HealthStatus Status { get; set; }
        public int Connections { get; set; }
        public int PoolSize { get; set; }
    }
}
