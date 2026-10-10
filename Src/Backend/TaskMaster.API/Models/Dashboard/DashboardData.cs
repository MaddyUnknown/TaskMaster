namespace TaskMaster.API.Models.Dashboard
{
    public class DashboardData
    {
        public int QueuedJobs { get; set; }
        public int InProgressJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public int ActiveWorkers { get; set; }
        public int InactiveWorkers { get; set; }
        public bool DatabaseHealthy { get; set; }
    }
}
