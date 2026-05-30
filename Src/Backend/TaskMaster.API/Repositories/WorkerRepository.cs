using Microsoft.EntityFrameworkCore;
using TaskMaster.Data;
using TaskMaster.Entities;
using TaskMaster.Interfaces.Repositories;

namespace TaskMaster.Repositories
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
            return await _context.Workers.Include(w => w.JobTypeCapabilities).Where(w => w.WorkerPublicId == workerPublicId).FirstOrDefaultAsync();
        }

        public async Task<Worker?> UpdateWorkerExpiryTimestampAsync(Guid workerPublicId, int workerExpiryIntervalSeconds)
        {
            FormattableString sql = $@"
                UPDATE Workers
                SET WorkerExpiresAtTimestamp = DATEADD(SECOND, {workerExpiryIntervalSeconds}, SYSDATETIME()), ModifyDateTime = {DateTime.Now}
                WHERE WorkerPublicId = {workerPublicId}
                AND WorkerExpiresAtTimestamp > SYSDATETIME()
            ";

            var count = await _context.Database.ExecuteSqlInterpolatedAsync(sql);

            if (count == 0) return null;

            return await _context.Workers
                .Include(w => w.JobTypeCapabilities)
                .FirstOrDefaultAsync(w => w.WorkerPublicId == workerPublicId);
        }
    }
}