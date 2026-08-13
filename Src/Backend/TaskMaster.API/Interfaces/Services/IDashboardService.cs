using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Models.Dashboard;

namespace TaskMaster.API.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<IEnumerable<ActivityItem>> GetRecentActivityAsync(int totalItems);
        Task<SystemMetrics> GetSystemMetricsAsync();
        Task<SystemHealth> GetSystemHealthAsync();
        Task<IEnumerable<JobStatsItem>> GetJobStatsAsync();
    }
}
