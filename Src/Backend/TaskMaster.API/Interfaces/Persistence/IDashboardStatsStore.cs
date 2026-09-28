using TaskMaster.API.Interfaces.Queries;

namespace TaskMaster.API.Interfaces.Persistence
{
    public interface IDashboardStatsStore
    {
        Task<DashboardData> GetDashboardCountsAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<JobStatsItem>> GetHourlyJobStatsAsync(CancellationToken cancellationToken = default);
    }
}
