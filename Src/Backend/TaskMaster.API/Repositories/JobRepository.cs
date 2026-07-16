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

        public async Task<int> UnassignJobsForInactiveWorkersAsync()
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                UPDATE j
                SET 
                    j.Status = {(int)JobStatusEnum.Queued}, 
                    j.AssignedWorkerId = NULL, 
                    j.ModifyDateTime = {currentDateTime}
                FROM Jobs j
                INNER JOIN Workers w ON w.Id = j.AssignedWorkerId
                WHERE w.Status = {(int)WorkerStatusEnum.InActive}
                AND j.Status = {(int)JobStatusEnum.InProgress};
            ";

            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }

        public async Task<Job?> GetNextJobForWorkerAsync(Guid workerPublicId)
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                WITH cte AS
                (
                    SELECT TOP 1 j.*
                    FROM Workers w
                    INNER JOIN WorkerCapabilities wc ON wc.WorkerId = w.Id AND w.WorkerPublicId = {workerPublicId} AND WorkerExpiresAtTimestamp > {currentDateTime} AND Status = {WorkerStatusEnum.Active}
                    INNER JOIN Jobs j WITH (UPDLOCK, READPAST, ROWLOCK) ON j.JobTypeId = wc.JobTypeId
                    WHERE j.Status = {JobStatusEnum.Queued}
                    ORDER BY j.Id
                )
                UPDATE cte
                SET Status = {JobStatusEnum.InProgress}, AssignedWorkerId = (SELECT Id FROM Workers WHERE WorkerPublicId = {workerPublicId}), ModifyDateTime = {currentDateTime}
                OUTPUT inserted.Id";

            var jobId = (await _context.Database.SqlQuery<long>(sql).ToListAsync()).FirstOrDefault();

            Job? job = null;
            if (jobId > 0)
            {
                job = await _context.Jobs
                    .Include(j => j.JobType)
                    .Include(j => j.AssignedWorker)
                    .FirstOrDefaultAsync(j => j.Id == jobId);
            }

            return job;
        }
    }
}
