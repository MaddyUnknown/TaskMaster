using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Queries;

namespace TaskMaster.API.Queries
{
    public class DashboardQuery : IDashboardQuery
    {
        private readonly ApplicationDbContext _context;

        public DashboardQuery(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<DashboardData> GetDashboardDataAsync()
        {
            try
            {
                var result = await _context.Database
                    .SqlQuery<DashboardCounts>(
                        $@"SELECT
                            (SELECT COUNT(*) FROM Jobs WHERE Status = {(int)JobStatusEnum.Queued}) AS QueuedJobs,
                            (SELECT COUNT(*) FROM Jobs WHERE Status = {(int)JobStatusEnum.InProgress}) AS InProgressJobs,
                            (SELECT COUNT(*) FROM Jobs WHERE Status = {(int)JobStatusEnum.Completed}) AS CompletedJobs,
                            (SELECT COUNT(*) FROM Jobs WHERE Status = {(int)JobStatusEnum.Failed}) AS FailedJobs,
                            (SELECT COUNT(*) FROM Workers WHERE Status = {(int)WorkerStatusEnum.Active}) AS ActiveWorkers,
                            (SELECT COUNT(*) FROM Workers WHERE Status = {(int)WorkerStatusEnum.InActive}) AS InactiveWorkers"
                    )
                    .SingleAsync();

                return new DashboardData
                {
                    QueuedJobs = result.QueuedJobs,
                    InProgressJobs = result.InProgressJobs,
                    CompletedJobs = result.CompletedJobs,
                    FailedJobs = result.FailedJobs,
                    ActiveWorkers = result.ActiveWorkers,
                    InactiveWorkers = result.InactiveWorkers,
                    DatabaseHealthy = true
                };
            }
            catch
            {
                return new DashboardData
                {
                    DatabaseHealthy = false
                };
            }
        }

        public async Task<List<Job>> GetRecentJobsAsync(int count)
        {
            return await _context.Jobs
                .Include(j => j.JobType)
                .OrderByDescending(j => j.ModifyDateTime)
                .Take(count)
                .ToListAsync();
        }

        private class DashboardCounts
        {
            public int QueuedJobs { get; set; }
            public int InProgressJobs { get; set; }
            public int CompletedJobs { get; set; }
            public int FailedJobs { get; set; }
            public int ActiveWorkers { get; set; }
            public int InactiveWorkers { get; set; }
        }
    }
}
