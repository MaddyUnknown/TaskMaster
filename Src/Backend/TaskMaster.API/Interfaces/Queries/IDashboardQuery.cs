using TaskMaster.API.Entities;
using TaskMaster.API.Models.Dashboard;

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

    public class JobStatsItem
    {
        public DateTime BucketStart { get; set; }
        public DateTime BucketEnd { get; set; }
        public string BucketHour { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }

    public interface IDashboardQuery
    {
        Task<DashboardData> GetDashboardDataAsync();
        Task<List<SystemActivity>> GetRecentSystemActivitiesAsync(int count);
        Task<IEnumerable<JobStatsItem>> GetJobStatsAsync();
    }
}
