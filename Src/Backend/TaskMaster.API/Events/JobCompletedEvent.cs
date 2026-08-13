namespace TaskMaster.API.Events
{
    public class JobCompletedEvent
    {
        public Guid JobId { get; set; }
        public string JobTypeName { get; set; } = string.Empty;
        public long JobTypeVersion { get; set; }
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
    }
}
