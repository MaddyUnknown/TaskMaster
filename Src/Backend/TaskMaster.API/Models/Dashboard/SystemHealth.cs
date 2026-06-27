using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Models.Dashboard
{
    public class SystemHealth
    {
        public ComponentHealth Api { get; set; } = new();
        public DatabaseHealth Database { get; set; } = new();
        public QueueHealth Queue { get; set; } = new();
        public WorkerHealth Workers { get; set; } = new();
    }

    public class ComponentHealth
    {
        public HealthStatus Status { get; set; }
        public string Uptime { get; set; } = string.Empty;
        public int Latency { get; set; }
    }

    public class DatabaseHealth
    {
        public HealthStatus Status { get; set; }
        public int Connections { get; set; }
        public int PoolSize { get; set; }
    }

    public class QueueHealth
    {
        public HealthStatus Status { get; set; }
        public int Depth { get; set; }
        public int Throughput { get; set; }
    }

    public class WorkerHealth
    {
        public HealthStatus Status { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
    }
}
