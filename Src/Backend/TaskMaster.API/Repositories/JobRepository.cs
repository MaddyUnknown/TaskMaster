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
            return await _context.Jobs
                .Include(j => j.JobType)
                .Where(j => j.JobPublicId == jobPublicId && j.AssignedWorker!.WorkerPublicId == workerPublicId)
                .FirstOrDefaultAsync();
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

        public async Task<Job?> GetNextJobForWorkerAsync(Guid workerPublicId)
        {
            var currentDateTime = DateTime.Now;

            FormattableString sql = $@"
                DECLARE @jobId TABLE (Id bigint NOT NULL);

                WITH cte AS
                (
                    SELECT TOP 1 j.*
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
                OUTPUT INSERTED.Id INTO @jobId;

                SELECT j.Id, j.JobPublicId, j.Payload, j.[Status], j.JobTypeId, j.AssignedWorkerId,
                       j.CreatedDateTime, j.ModifyDateTime,
                       jt.Id AS JobType_Id, jt.Name AS JobType_Name, jt.Version AS JobType_Version,
                       jt.[Schema] AS JobType_Schema, jt.Description AS JobType_Description,
                       jt.CreatedDateTime AS JobType_CreatedDateTime, jt.ModifyDateTime AS JobType_ModifyDateTime
                FROM Jobs j
                INNER JOIN JobTypes jt ON jt.Id = j.JobTypeId
                WHERE j.Id IN (SELECT Id FROM @jobId);
            ";

            var result = await _context.Database.SqlQuery<JobPullDto>(sql).ToListAsync();

            var dto = result.FirstOrDefault();
            if (dto == null) return null;

            return new Job
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
            };
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
