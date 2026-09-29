using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;

namespace TaskMaster.API.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly IApplicationDbContext _context;
        private readonly IJobClaimStore _claimStore;

        public JobRepository(IApplicationDbContext context, IJobClaimStore claimStore)
        {
            _context = context;
            _claimStore = claimStore;
        }

        public async Task<Job?> GetByJobPublicIdAsync(Guid jobPublicId)
        {
            return await _context.Jobs.Include(j => j.JobType).Include(j => j.AssignedWorker).Where(j => j.JobPublicId == jobPublicId).FirstOrDefaultAsync();
        }

        public async Task<PagedResult<Job>> GetAllJobsAsync(JobQuery query)
        {
            var jobsQuery = _context.Jobs.Include(j => j.JobType).Include(j => j.AssignedWorker).AsQueryable();

            if (query.Status.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.Status == query.Status.Value);
            }

            if (query.Page == null || query.PageSize == null)
            {
                var all = await jobsQuery.OrderByDescending(j => j.Id).ToListAsync();
                return PagedResult<Job>.Unpaged(all);
            }

            var page = query.Page.Value;
            var pageSize = query.PageSize.Value;

            var totalCount = await jobsQuery.CountAsync();

            var items = await jobsQuery
                .OrderByDescending(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PagedResult<Job>.Create(items, page, pageSize, totalCount);
        }

        public async Task<Job?> GetByJobPublicIdAndWorkerPublicIdAsync(Guid jobPublicId, Guid workerPublicId)
        {
            return await _context.Jobs
                .Include(j => j.JobType)
                .Where(j => j.JobPublicId == jobPublicId && j.AssignedWorker!.WorkerPublicId == workerPublicId)
                .FirstOrDefaultAsync();
        }

        public async Task<JobStatusCounts> CountJobsByStatusAsync()
        {
            var grouped = await _context.Jobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return new JobStatusCounts
            {
                Queued = grouped.FirstOrDefault(x => x.Status == JobStatusEnum.Queued)?.Count ?? 0,
                InProgress = grouped.FirstOrDefault(x => x.Status == JobStatusEnum.InProgress)?.Count ?? 0,
                Completed = grouped.FirstOrDefault(x => x.Status == JobStatusEnum.Completed)?.Count ?? 0,
                Failed = grouped.FirstOrDefault(x => x.Status == JobStatusEnum.Failed)?.Count ?? 0
            };
        }

        public Task<int> UnassignJobForWorkerIdAsync(long workerId)
        {
            return _claimStore.UnassignJobsForWorkerIdAsync(workerId);
        }

        public Task<int> UnassignJobsForInactiveWorkersAsync()
        {
            return _claimStore.UnassignJobsForInactiveWorkersAsync();
        }

        public Task<List<Job>> GetNextJobsForWorkerAsync(Guid workerPublicId, int maxJobs)
        {
            return _claimStore.ClaimJobsAsync(workerPublicId, maxJobs);
        }

        public async Task<int> UpdateJobStatusAsync(BulkUpdateJobStatus request)
        {
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerPublicId == request.WorkerId);
            if (worker == null) return 0;

            var count = 0;
            var now = DateTime.UtcNow;

            foreach (var item in request.JobStatuses)
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobPublicId == item.JobId && j.AssignedWorkerId == worker.Id);
                if (job == null) continue;

                job.Status = item.Status;

                job.ModifyDateTime = now;
                count++;
            }

            await _context.SaveChangesAsync();
            return count;
        }
    }
}
