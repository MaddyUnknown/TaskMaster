using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Enums;
using TaskMaster.Data;
using TaskMaster.Entities;
using TaskMaster.Enums;
using TaskMaster.Interfaces.Repositories;

namespace TaskMaster.Repositories
{
    public class JobRepository : IJobRepository
    {
        private ApplicationDbContext _context;
        public JobRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId)
        {
            var workerId = await _context.Workers.Where(w => w.WorkerPublicId == workerPublicId).Select(w => w.Id).FirstOrDefaultAsync();
            if (workerId == 0) return null;

            return await _context.Jobs.Where(j => j.JobPublicId == jobPublicId && j.AssignedWorkerId == workerId).FirstOrDefaultAsync();
        }

        public async Task<int> UnassignJobForWorkerId(long workerId)
        {
            FormattableString sql = $@"
                UPDATE Jobs SET
                    Status = {(int)JobStatusEnum.Queued},
                    AssignedWorkerId = NULL,
                    ModifyDateTime = {DateTime.Now}
                WHERE j.AssignedWorkerId = {workerId};
            ";

            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }

        public async Task<Job?> GetNextJobForWorkerAsync(long workerId)
        {
            FormattableString sql = $@"
                DECLARE @ClaimedJob TABLE
                (
                    Id INT
                );

                WITH cte AS
                (
                    SELECT TOP 1 j.*
                    FROM Workers w
                    INNER JOIN WorkerCapabalities wc ON wc.WorkerId = w.Id AND w.Id = {workerId} AND WorkerExpiresAtTimestamp > SYSDATETIME() AND Status = {WorkerStatusEnum.Active}
                    INNER JOIN Jobs j WITH (UPDLOCK, READPAST, ROWLOCK) ON j.JobType = wc.JobType
                    WHERE j.Status = {JobStatusEnum.Queued}
                    ORDER BY j.Id
                )
                UPDATE cte
                SET Status = {JobStatusEnum.InProgress}, AssignedWorkerId = {workerId}, ModifyDateTime = {DateTime.Now}
                OUTPUT inserted.Id INTO @ClaimedJob;

                SELECT j.* 
                FROM Jobs j
                INNER JOIN @ClaimedJob c ON j.Id = c.Id;
            ";

            var jobs = await _context.Jobs.FromSqlInterpolated(sql).ToListAsync();
            return jobs.FirstOrDefault();
        }
    }
}
