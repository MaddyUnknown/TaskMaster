using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Persistence;
using TaskMaster.API.Interfaces.Queries;

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

        public async Task<IEnumerable<JobStatsItem>> GetHourlyJobStatsAsync(CancellationToken cancellationToken = default)
        {
            var raw = await _context.Database.SqlQuery<HourlyCount>($@"
                WITH RECURSIVE Params AS
                (
                    SELECT
                        DATE_TRUNC('hour', CURRENT_TIMESTAMP)
                        - (EXTRACT(hour FROM CURRENT_TIMESTAMP)::int % 2) * INTERVAL '1 hour'
                        AS ""CurrentBucketStart""
                ),
                Buckets AS
                (
                     SELECT
                        ""CurrentBucketStart"" - INTERVAL '22 hours' AS ""BucketStart""
                    FROM Params

                    UNION ALL

                    SELECT
                        b.""BucketStart"" + INTERVAL '2 hours'
                    FROM Buckets b
                    CROSS JOIN Params p
                    WHERE b.""BucketStart"" < p.""CurrentBucketStart""
                ),
                JobCounts AS
                (
                    SELECT
                        DATE_TRUNC('hour', j.""CreatedDateTime"") - (EXTRACT(hour FROM j.""CreatedDateTime"")::int % 2) * INTERVAL '1 hour' AS ""BucketStart"",
                        COUNT(*) AS ""JobCount""
                    FROM ""Jobs"" j
                    CROSS JOIN Params p
                    WHERE
                        j.""CreatedDateTime"" >= p.""CurrentBucketStart"" - INTERVAL '22 hours'
                        AND j.""CreatedDateTime"" < CURRENT_TIMESTAMP
                    GROUP BY
                        DATE_TRUC('hour', j.""CreatedDateTime"")
                        - (EXTRACT(hour FROM j.""CreatedDateTime"")::int % 2) * INTERVAL '1 hour'
                )
                SELECT
                    b.""BucketStart"",
                    b.""BucketStart"" + INTERVAL '2 hours' ""BucketEnd"",
                    TO_CHAR(b.""BucketStart"", 'HH24:MI') AS ""BucketHour"",
                    COALESCE(j.JobCount, 0) AS ""JobCount""
                FROM Buckets b
                LEFT JOIN JobCounts j
                    ON b.""BucketStart"" = j.""BucketStart""
                ORDER BY b.""BucketStart"";"
            ).ToListAsync(cancellationToken);

            return raw.Select(r => new JobStatsItem
            {
                BucketStart = r.BucketStart,
                BucketEnd = r.BucketEnd,
                BucketHour = r.BucketHour,
                JobCount = r.JobCount
            });
        }

        private sealed class HourlyCount
        {
            public DateTime BucketStart { get; set; }
            public DateTime BucketEnd { get; set; }
            public string BucketHour { get; set; } = string.Empty;
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
