using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;

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

        public async Task<PagedResult<Worker>> GetAllWorkersAsync(WorkerQuery query)
        {
            var workersQuery = _context.Workers.Include(w => w.WorkerCapabilities).ThenInclude(wc => wc.JobType).AsQueryable();

            if (query.Status.HasValue)
            {
                workersQuery = workersQuery.Where(w => w.Status == query.Status.Value);
            }

            if (query.Page == null || query.PageSize == null)
            {
                var all = await workersQuery.OrderByDescending(w => w.Id).ToListAsync();
                return PagedResult<Worker>.Unpaged(all);
            }

            var page = query.Page.Value;
            var pageSize = query.PageSize.Value;

            var totalCount = await workersQuery.CountAsync();

            var items = await workersQuery
                .OrderByDescending(w => w.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PagedResult<Worker>.Create(items, page, pageSize, totalCount);
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

        public async Task<Worker?> UpdateWorkerExpiryAndReturnAsync(Guid workerPublicId, int workerExpiryIntervalSeconds)
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                UPDATE Workers
                SET 
                    LastHeartBeatTimestamp = {currentDateTime},
                    WorkerExpiresAtTimestamp = {currentDateTime.AddSeconds(workerExpiryIntervalSeconds)}, 
                    ModifyDateTime = {currentDateTime}
                OUTPUT inserted.*
                WHERE WorkerPublicId = {workerPublicId}
                AND WorkerExpiresAtTimestamp > {currentDateTime}
            ";

            var workers = await _context.Workers.FromSqlInterpolated(sql).ToListAsync();
            return workers.FirstOrDefault();
        }

        public async Task<WorkerStatusCounts> CountWorkersByStatusAsync()
        {
            var grouped = await _context.Workers
                .GroupBy(w => w.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return new WorkerStatusCounts
            {
                Active = grouped.FirstOrDefault(x => x.Status == WorkerStatusEnum.Active)?.Count ?? 0,
                InActive = grouped.FirstOrDefault(x => x.Status == WorkerStatusEnum.InActive)?.Count ?? 0
            };
        }

        public async Task<int> DeactivateExpiredWorkersAsync()
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                UPDATE Workers
                SET 
                    Status = {(int)WorkerStatusEnum.InActive}, 
                    ModifyDateTime = {currentDateTime}
                WHERE Status = {(int)WorkerStatusEnum.Active}
                AND WorkerExpiresAtTimestamp <= {currentDateTime};
            ";

            return await _context.Database.ExecuteSqlInterpolatedAsync(sql);
        }

        public async Task<List<Worker>> GetExpiredActiveWorkersAsync()
        {
            var currentDateTime = DateTime.Now;

            return await _context.Workers
                .Where(w => w.Status == WorkerStatusEnum.Active && w.WorkerExpiresAtTimestamp <= currentDateTime)
                .ToListAsync();
        }
    }
}