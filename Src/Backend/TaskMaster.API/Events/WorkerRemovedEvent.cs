namespace TaskMaster.API.Events
{
    public class WorkerRemovedEvent
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
    }
}
