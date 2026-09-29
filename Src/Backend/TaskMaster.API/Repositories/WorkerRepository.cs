using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Repositories
{
    public class WorkerRepository : IWorkerRepository
    {
        private readonly IApplicationDbContext _context;
        private readonly IWorkerStore _workerStore;

        public WorkerRepository(IApplicationDbContext context, IWorkerStore workerStore)
        {
            _context = context;
            _workerStore = workerStore;
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

        public Task<Worker?> GetByWorkerNameAsync(string workerName, bool withLock = false)
        {
            return _workerStore.GetByWorkerNameAsync(workerName, withLock);
        }

        public Task<Worker?> UpdateWorkerExpiryAndReturnAsync(Guid workerPublicId, int workerExpiryIntervalSeconds)
        {
            return _workerStore.UpdateWorkerExpiryAndReturnAsync(workerPublicId, workerExpiryIntervalSeconds);
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

        public Task<int> DeactivateExpiredWorkersAsync()
        {
            return _workerStore.DeactivateExpiredWorkersAsync();
        }

        public async Task<List<Worker>> GetExpiredActiveWorkersAsync()
        {
            var currentDateTime = DateTime.UtcNow;

            return await _context.Workers
                .Where(w => w.Status == WorkerStatusEnum.Active && w.WorkerExpiresAtTimestamp <= currentDateTime)
                .ToListAsync();
        }
    }
}
