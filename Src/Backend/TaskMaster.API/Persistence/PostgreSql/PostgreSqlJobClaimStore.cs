using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Models.Jobs;

namespace TaskMaster.API.Persistence.PostgreSql
{
    public class PostgreSqlJobClaimStore : IJobClaimStore
    {
        private readonly IApplicationDbContext _context;

        public PostgreSqlJobClaimStore(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Job>> ClaimJobsAsync(
            Guid workerPublicId,
            int maxJobs,
            CancellationToken cancellationToken = default)
        {
            var currentDateTime = DateTime.UtcNow;

            FormattableString sql = $@"
                WITH cte AS
                (
                    SELECT j.""Id"" AS ""JobId"", w.""Id"" AS ""WorkerId""
                    FROM ""Workers"" w
                    INNER JOIN ""WorkerCapabilities"" wc
                        ON wc.""WorkerId"" = w.""Id""
                        AND w.""WorkerPublicId"" = {workerPublicId}
                        AND ""WorkerExpiresAtTimestamp"" > {currentDateTime}
                        AND ""Status"" = {(int)WorkerStatusEnum.Active}
                    INNER JOIN ""Jobs"" j
                        ON j.""JobTypeId"" = wc.""JobTypeId""
                    WHERE j.""Status"" = {(int)JobStatusEnum.Queued}
                    ORDER BY j.""Id""
                    LIMIT {maxJobs}
                    FOR UPDATE OF j SKIP LOCKED
                ),
                jobIds AS
                (
                    UPDATE ""Jobs"" j
                    SET
                        ""Status"" = {(int)JobStatusEnum.InProgress},
                        ""AssignedWorkerId"" = c.""WorkerId"",
                        ""ModifyDateTime"" = {currentDateTime}
                    FROM cte c
                    WHERE j.""Id"" = c.""JobId""
                    RETURNING j.""Id""
                )
                SELECT j.""Id"", j.""JobPublicId"", j.""Payload"", j.""Status"", j.""JobTypeId"", j.""AssignedWorkerId"",
                       j.""CreatedDateTime"", j.""ModifyDateTime"",
                       jt.""Id"" AS ""JobType_Id"", jt.""Name"" AS ""JobType_Name"", jt.""Version"" AS ""JobType_Version"",
                       jt.""Schema"" AS ""JobType_Schema"", jt.""Description"" AS ""JobType_Description"",
                       jt.""CreatedDateTime"" AS ""JobType_CreatedDateTime"", jt.""ModifyDateTime"" AS ""JobType_ModifyDateTime""
                FROM ""Jobs"" j
                INNER JOIN ""JobTypes"" jt ON jt.""Id"" = j.""JobTypeId""
                INNER JOIN jobIds selectedJob ON j.""Id"" = selectedJob.""Id"";
            ";

            var result = await _context.Database
                .SqlQuery<JobPullDto>(sql)
                .ToListAsync(cancellationToken);

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

        public Task<int> UnassignJobsForInactiveWorkersAsync(CancellationToken cancellationToken = default)
        {
            var currentDateTime = DateTime.UtcNow;

            FormattableString sql = $@"
                UPDATE ""Jobs"" AS j
                SET
                    j.""Status"" = {(int)JobStatusEnum.Queued},
                    j.""AssignedWorkerId"" = NULL,
                    j.""ModifyDateTime"" = {currentDateTime}
                FROM ""Workers"" w
                WHERE w.""Id"" = j.""AssignedWorkerId""
                AND w.""Status"" = {(int)WorkerStatusEnum.InActive}
                AND j.""Status"" = {(int)JobStatusEnum.InProgress};
            ";

            return _context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
        }

        public Task<int> UnassignJobsForWorkerIdAsync(long workerId, CancellationToken cancellationToken = default)
        {
            FormattableString sql = $@"
                UPDATE ""Jobs"" SET
                    ""Status"" = {(int)JobStatusEnum.Queued},
                    ""AssignedWorkerId"" = NULL,
                    ""ModifyDateTime"" = {DateTime.UtcNow}
                WHERE ""AssignedWorkerId"" = {workerId}
                AND ""Status"" = {(int)JobStatusEnum.InProgress};
            ";

            return _context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
        }
    }
}
