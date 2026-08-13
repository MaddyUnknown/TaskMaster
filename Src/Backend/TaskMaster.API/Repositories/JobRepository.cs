using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;

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

        public async Task<int> UnassignJobForWorkerIdAsync(long workerId)
        {
            FormattableString sql = $@"
                UPDATE Jobs SET
                    Status = {(int)JobStatusEnum.Queued},
                    AssignedWorkerId = NULL,
                    ModifyDateTime = {DateTime.Now}
                WHERE AssignedWorkerId = {workerId}
                AND Status = {(int)JobStatusEnum.InProgress};
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

        public async Task<List<Job>> GetNextJobsForWorkerAsync(Guid workerPublicId, int maxJobs)
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                DECLARE @jobIds TABLE (Id bigint NOT NULL);

                WITH cte AS
                (
                    SELECT TOP ({maxJobs}) j.*
                    FROM Workers w
                    INNER JOIN WorkerCapabilities wc 
                        ON wc.WorkerId = w.Id 
                        AND w.WorkerPublicId = {workerPublicId} 
                        AND WorkerExpiresAtTimestamp > {currentDateTime} 
                        AND Status = {(int)WorkerStatusEnum.Active}
                    INNER JOIN Jobs j WITH (UPDLOCK, READPAST, ROWLOCK) 
                        ON j.JobTypeId = wc.JobTypeId
                    WHERE j.Status = {(int)JobStatusEnum.Queued}
                    ORDER BY j.Id
                )
                UPDATE cte
                SET 
                    Status = {(int)JobStatusEnum.InProgress}, 
                    AssignedWorkerId = (SELECT Id FROM Workers WHERE WorkerPublicId = {workerPublicId}), 
                    ModifyDateTime = {currentDateTime}
                OUTPUT INSERTED.Id INTO @jobIds;

                SELECT j.Id, j.JobPublicId, j.Payload, j.[Status], j.JobTypeId, j.AssignedWorkerId,
                       j.CreatedDateTime, j.ModifyDateTime,
                       jt.Id AS JobType_Id, jt.Name AS JobType_Name, jt.Version AS JobType_Version,
                       jt.[Schema] AS JobType_Schema, jt.Description AS JobType_Description,
                       jt.CreatedDateTime AS JobType_CreatedDateTime, jt.ModifyDateTime AS JobType_ModifyDateTime
                FROM Jobs j
                INNER JOIN JobTypes jt ON jt.Id = j.JobTypeId
                WHERE j.Id IN (SELECT Id FROM @jobIds);
            ";

            var result = await _context.Database.SqlQuery<JobPullDto>(sql).ToListAsync();

            return result.Select(dto => new Job
            {
                Id = dto.Id,
                JobPublicId = dto.JobPublicId,
                Payload = dto.Payload,
                Status = dto.Status,
                JobTypeId = dto.JobTypeId,
                AssignedWorkerId = dto.AssignedWorkerId,
                CreatedDateTime = dto.CreatedDateTime,
                ModifyDateTime = dto.ModifyDateTime,
                JobType = new JobType
                {
                    Id = dto.JobType_Id,
                    Name = dto.JobType_Name,
                    Version = dto.JobType_Version,
                    Schema = dto.JobType_Schema,
                    Description = dto.JobType_Description,
                    CreatedDateTime = dto.JobType_CreatedDateTime,
                    ModifyDateTime = dto.JobType_ModifyDateTime
                }
            }).ToList();
        }

        public async Task<int> UpdateJobStatusAsync(BulkUpdateJobStatus request)
        {
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerPublicId == request.WorkerId);
            if (worker == null) return 0;

            var count = 0;
            var now = DateTime.Now;

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

        private sealed class JobPullDto
        {
            public long Id { get; set; }
            public Guid JobPublicId { get; set; }
            public string? Payload { get; set; }
            public JobStatusEnum Status { get; set; }
            public long JobTypeId { get; set; }
            public long? AssignedWorkerId { get; set; }
            public DateTime CreatedDateTime { get; set; }
            public DateTime? ModifyDateTime { get; set; }

            public long JobType_Id { get; set; }
            public string JobType_Name { get; set; } = string.Empty;
            public long JobType_Version { get; set; }
            public string JobType_Schema { get; set; } = string.Empty;
            public string? JobType_Description { get; set; }
            public DateTime JobType_CreatedDateTime { get; set; }
            public DateTime? JobType_ModifyDateTime { get; set; }
        }
    }
}
