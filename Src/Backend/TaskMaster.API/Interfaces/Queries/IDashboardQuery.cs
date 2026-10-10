using TaskMaster.API.Entities;
using TaskMaster.API.Models.Dashboard;

namespace TaskMaster.API.Interfaces.Queries
{
    public interface IDashboardQuery
    {
        Task<DashboardData> GetDashboardDataAsync();
        Task<List<SystemActivity>> GetRecentSystemActivitiesAsync(int count);
        Task<JobStatsResponse> GetJobStatsAsync(string timeZone);
    }
}
