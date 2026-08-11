namespace TaskMaster.API.Events
{
    public class WorkerRegisteredEvent
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
    }
}
