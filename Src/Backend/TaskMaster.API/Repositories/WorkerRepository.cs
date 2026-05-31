using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Repositories;

namespace TaskMaster.API.Repositories
{
    public class WorkerRepository : IWorkerRepository
    {
        private ApplicationDbContext _context;
        public WorkerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Worker?> GetByPublicIdAsync(Guid workerPublicId)
        {
            return await _context.Workers.Include(w => w.WorkerCapabilities).ThenInclude(wc => wc.JobType).Where(w => w.WorkerPublicId == workerPublicId).FirstOrDefaultAsync();
        }

        public async Task<int> UpdateWorkerExpiryTimestampAsync(Guid workerPublicId, int workerExpiryIntervalSeconds)
        {
            FormattableString sql = $@"
                UPDATE Workers
                SET WorkerExpiresAtTimestamp = DATEADD(SECOND, {workerExpiryIntervalSeconds}, SYSDATETIME()), ModifyDateTime = {DateTime.Now}
                WHERE WorkerPublicId = {workerPublicId}
                AND WorkerExpiresAtTimestamp > SYSDATETIME()
            ";

            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }
    }
}