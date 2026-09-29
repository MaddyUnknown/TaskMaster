using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Persistence;

namespace TaskMaster.API.Persistence.PostgreSql
{
    public class PostgreSqlWorkerStore : IWorkerStore
    {
        private readonly IApplicationDbContext _context;

        public PostgreSqlWorkerStore(IApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Worker?> GetByWorkerNameAsync(
            string workerName,
            bool withLock = false,
            CancellationToken cancellationToken = default)
        {
            FormattableString sql = withLock
                ? (FormattableString) $@"SELECT * FROM ""Workers"" WHERE ""WorkerName"" = {workerName} FOR UPDATE"
                : (FormattableString) $@"SELECT * FROM ""Workers"" WHERE ""WorkerName"" = {workerName}";

            return _context.Workers
                .FromSqlInterpolated(sql)
                .Include(w => w.WorkerCapabilities)
                .ThenInclude(wc => wc.JobType)
                .FirstOrDefaultAsync(cancellationToken)!;
        }

        public async Task<Worker?> UpdateWorkerExpiryAndReturnAsync(
            Guid workerPublicId,
            int workerExpiryIntervalSeconds,
            CancellationToken cancellationToken = default)
        {
            var currentDateTime = DateTime.UtcNow;

            FormattableString sql = $@"
                UPDATE ""Workers""
                SET
                    ""LastHeartBeatTimestamp"" = {currentDateTime},
                    ""WorkerExpiresAtTimestamp"" = {currentDateTime.AddSeconds(workerExpiryIntervalSeconds)},
                    ""ModifyDateTime"" = {currentDateTime}
                WHERE ""WorkerPublicId"" = {workerPublicId}
                AND ""WorkerExpiresAtTimestamp"" > {currentDateTime}
                RETURNING *;
            ";

            var workers = await _context.Workers
                .FromSqlInterpolated(sql)
                .ToListAsync(cancellationToken);

            return workers.FirstOrDefault();
        }

        public Task<int> DeactivateExpiredWorkersAsync(CancellationToken cancellationToken = default)
        {
            var currentDateTime = DateTime.UtcNow;

            FormattableString sql = $@"
                UPDATE ""Workers""
                SET
                    ""Status"" = {(int)WorkerStatusEnum.InActive},
                    ""ModifyDateTime"" = {currentDateTime}
                WHERE ""Status"" = {(int)WorkerStatusEnum.Active}
                AND ""WorkerExpiresAtTimestamp"" <= {currentDateTime};
            ";

            return _context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
        }
    }
}
