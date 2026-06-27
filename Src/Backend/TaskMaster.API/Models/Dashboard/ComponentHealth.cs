using TaskMaster.API.Models.Enums;

namespace TaskMaster.API.Models.Dashboard
{
    public class ComponentHealth
    {
        public HealthStatus Status { get; set; }
        public string Uptime { get; set; } = string.Empty;
        public int Latency { get; set; }
    }
}
