namespace TaskMaster.API.Events
{
    public class WorkerInactiveEvent
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
    }
}
