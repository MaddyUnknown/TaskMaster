namespace TaskMaster.API.Models.Dashboard
{
    public class SystemHealth
    {
        public ComponentHealth Api { get; set; } = new();
        public DatabaseHealth Database { get; set; } = new();
        public QueueHealth Queue { get; set; } = new();
        public WorkerHealth Workers { get; set; } = new();
    }
}
