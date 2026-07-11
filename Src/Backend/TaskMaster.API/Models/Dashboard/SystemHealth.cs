namespace TaskMaster.API.Models.Dashboard
{
    public class SystemHealth
    {
        public ComponentHealth Api { get; set; } = new();
        public ComponentHealth Database { get; set; } = new();
        public ComponentHealth Workers { get; set; } = new();
    }
}
