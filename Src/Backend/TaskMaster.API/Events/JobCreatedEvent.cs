namespace TaskMaster.API.Events
{
    public class JobCreatedEvent
    {
        public Guid JobId { get; set; }
        public string JobTypeName { get; set; } = string.Empty;
        public long JobTypeVersion { get; set; }
    }
}
