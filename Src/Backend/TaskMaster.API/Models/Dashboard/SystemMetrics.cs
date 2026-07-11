namespace TaskMaster.API.Models.Dashboard
{
    public class SystemMetrics
    {
        public int ActiveWorkers { get; set; }
        public int TotalJobs { get; set; }
        public int QueuedJobs { get; set; }

    }
}
