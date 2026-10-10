using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Models.Dashboard;

namespace TaskMaster.API.Persistence.PostgreSql
{
    public class PostgreSqlDashboardStatsStore : IDashboardStatsStore
    {
        private readonly IApplicationDbContext _context;

        public PostgreSqlDashboardStatsStore(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardData> GetDashboardCountsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Database
                    .SqlQuery<DashboardCounts>(
                        $@"SELECT
                            (SELECT COUNT(*) FROM ""Jobs"" WHERE ""Status"" = {(int)JobStatusEnum.Queued}) AS ""QueuedJobs"",
                            (SELECT COUNT(*) FROM ""Jobs"" WHERE ""Status"" = {(int)JobStatusEnum.InProgress}) AS ""InProgressJobs"",
                            (SELECT COUNT(*) FROM ""Jobs"" WHERE ""Status"" = {(int)JobStatusEnum.Completed}) AS ""CompletedJobs"",
                            (SELECT COUNT(*) FROM ""Jobs"" WHERE ""Status"" = {(int)JobStatusEnum.Failed}) AS ""FailedJobs"",
                            (SELECT COUNT(*) FROM ""Workers"" WHERE ""Status"" = {(int)WorkerStatusEnum.Active}) AS ""ActiveWorkers"",
                            (SELECT COUNT(*) FROM ""Workers"" WHERE ""Status"" = {(int)WorkerStatusEnum.InActive}) AS ""InactiveWorkers"""
                    )
                    .SingleAsync(cancellationToken);

                return new DashboardData
                {
                    QueuedJobs = result.QueuedJobs,
                    InProgressJobs = result.InProgressJobs,
                    CompletedJobs = result.CompletedJobs,
                    FailedJobs = result.FailedJobs,
                    ActiveWorkers = result.ActiveWorkers,
                    InactiveWorkers = result.InactiveWorkers,
                    DatabaseHealthy = true
                };
            }
            catch
            {
                return new DashboardData
                {
                    DatabaseHealthy = false
                };
            }
        }

        public async Task<JobStatsResponse> GetHourlyJobStatsAsync(string timeZone, CancellationToken cancellationToken = default)
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            var nowUtc = DateTime.UtcNow;
            var buckets = JobStatsBucketer.CreateBuckets(timeZoneInfo, nowUtc);
            var starts = buckets.Select(b => b.BucketStart).ToArray();
            var ends = buckets.Select(b => b.BucketEnd).ToArray();

            var raw = await _context.Database
                .SqlQuery<BucketCount>(
                    $@"SELECT
                            b.""BucketIndex"" - 1 AS ""BucketIndex"",
                            COUNT(j.""Id"")::int AS ""JobCount""
                        FROM unnest({starts}, {ends}) WITH ORDINALITY AS b(""StartUtc"", ""EndUtc"", ""BucketIndex"")
                        LEFT JOIN ""Jobs"" j
                            ON j.""CreatedDateTime"" >= b.""StartUtc""
                            AND j.""CreatedDateTime"" < b.""EndUtc""
                        GROUP BY b.""BucketIndex""
                        ORDER BY b.""BucketIndex"";")
                .ToListAsync(cancellationToken);

            return JobStatsBucketer.BuildResponse(
                buckets,
                raw.ToDictionary(r => r.BucketIndex, r => r.JobCount),
                timeZoneInfo);
        }

        private sealed class BucketCount
        {
            public int BucketIndex { get; set; }
            public int JobCount { get; set; }
        }

        private class DashboardCounts
        {
            public int QueuedJobs { get; set; }
            public int InProgressJobs { get; set; }
            public int CompletedJobs { get; set; }
            public int FailedJobs { get; set; }
            public int ActiveWorkers { get; set; }
            public int InactiveWorkers { get; set; }
        }
    }
}