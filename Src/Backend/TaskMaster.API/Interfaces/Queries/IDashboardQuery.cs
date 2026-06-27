using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Queries
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

    public interface IDashboardQuery
    {
        Task<DashboardData> GetDashboardDataAsync();
        Task<List<Job>> GetRecentJobsAsync(int count);
    }
}
