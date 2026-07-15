using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Repositories;

namespace TaskMaster.API.Repositories
{
    public class JobRepository : IJobRepository
    {
        private ApplicationDbContext _context;
        public JobRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Job?> GetByJobPublicIdAsync(Guid jobPublicId)
        {
            return await _context.Jobs.Include(j => j.JobType).Include(j => j.AssignedWorker).Where(j => j.JobPublicId == jobPublicId).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            return await _context.Jobs.Include(j => j.JobType).Include(j => j.AssignedWorker).OrderByDescending(j => j.Id).ToListAsync();
        }

        public async Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId)
        {
            var workerId = await _context.Workers.Where(w => w.WorkerPublicId == workerPublicId).Select(w => w.Id).FirstOrDefaultAsync();
            if (workerId == 0) return null;

            return await _context.Jobs.Include(j => j.JobType).Include(j => j.AssignedWorker).Where(j => j.JobPublicId == jobPublicId && j.AssignedWorkerId == workerId).FirstOrDefaultAsync();
        }

        public async Task<int> UnassignJobForWorkerIdAsync(long workerId)
        {
            FormattableString sql = $@"
                UPDATE Jobs SET
                    Status = {(int)JobStatusEnum.Queued},
                    AssignedWorkerId = NULL,
                    ModifyDateTime = {DateTime.Now}
                WHERE AssignedWorkerId = {workerId};
            ";

            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }

        public async Task<Job?> GetNextJobForWorkerAsync(long workerId)
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                WITH cte AS
                (
                    SELECT TOP 1 j.*
                    FROM Workers w
                    INNER JOIN WorkerCapabilities wc ON wc.WorkerId = w.Id AND w.Id = {workerId} AND WorkerExpiresAtTimestamp > {currentDateTime} AND Status = {WorkerStatusEnum.Active}
                    INNER JOIN Jobs j WITH (UPDLOCK, READPAST, ROWLOCK) ON j.JobTypeId = wc.JobTypeId
                    WHERE j.Status = {JobStatusEnum.Queued}
                    ORDER BY j.Id
                )
                UPDATE cte
                SET Status = {JobStatusEnum.InProgress}, AssignedWorkerId = {workerId}, ModifyDateTime = {currentDateTime}
                OUTPUT inserted.*;
            ";

            var jobs = await _context.Jobs.FromSqlInterpolated(sql).ToListAsync();
            var job = jobs.FirstOrDefault();

            if(job != null)
            {
                await _context.Entry(job).Reference(j => j.JobType).LoadAsync();
                await _context.Entry(job).Reference(j => j.AssignedWorker).LoadAsync();
            }

            return job;
        }
    }
}
