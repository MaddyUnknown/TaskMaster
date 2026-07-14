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

        public async Task<IEnumerable<Worker>> GetAllWorkersAsync()
        {
            return await _context.Workers.Include(w => w.WorkerCapabilities).ThenInclude(wc => wc.JobType).OrderByDescending(w => w.Id).ToListAsync();
        }

        public async Task<Worker?> GetByWorkerNameAsync(string workerName, bool withLock = false)
        {
            var sql = withLock
                ? $"SELECT * FROM Workers WITH (UPDLOCK) WHERE WorkerName = {{0}}"
                : $"SELECT * FROM Workers WHERE WorkerName = {{0}}";

            return await _context.Workers
                .FromSqlRaw(sql, workerName)
                .Include(w => w.WorkerCapabilities)
                .ThenInclude(wc => wc.JobType)
                .FirstOrDefaultAsync();
        }

        public async Task<int> UpdateWorkerExpiryTimestampAsync(Guid workerPublicId, int workerExpiryIntervalSeconds)
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                UPDATE Workers
                SET 
                    LastHeartBeatTimestamp = {currentDateTime},
                    WorkerExpiresAtTimestamp = {currentDateTime.AddSeconds(workerExpiryIntervalSeconds)}, 
                    ModifyDateTime = {currentDateTime}
                WHERE WorkerPublicId = {workerPublicId}
                AND WorkerExpiresAtTimestamp <= {currentDateTime}
            ";

            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }
    }
}