using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Queries;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Models.Dashboard;


namespace TaskMaster.API.Queries
{
    public class DashboardQuery : IDashboardQuery
    {
        private readonly IApplicationDbContext _context;
        private readonly IDashboardStatsStore _statsStore;

        public DashboardQuery(IApplicationDbContext context, IDashboardStatsStore statsStore)
        {
            _context = context;
            _statsStore = statsStore;
        }

        public Task<DashboardData> GetDashboardDataAsync()
        {
            return _statsStore.GetDashboardCountsAsync();
        }

        public Task<JobStatsResponse> GetJobStatsAsync(string timeZone)
        {
            return _statsStore.GetHourlyJobStatsAsync(timeZone);
        }

        public async Task<List<SystemActivity>> GetRecentSystemActivitiesAsync(int count)
        {
            return await _context.SystemActivities
                .OrderByDescending(a => a.CreatedDateTime)
                .Take(count)
                .ToListAsync();
        }
    }
}
