namespace TaskMaster.API.Models.Workers
{
    public class WorkerRef
    {
        public Guid WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
    }
}
