using TaskMaster.API.Models.Dashboard;

namespace TaskMaster.API.Interfaces.Persistence
{
    public interface IDashboardStatsStore
    {
        Task<DashboardData> GetDashboardCountsAsync(CancellationToken cancellationToken = default);

        Task<JobStatsResponse> GetHourlyJobStatsAsync(string timeZone, CancellationToken cancellationToken = default);
    }
}