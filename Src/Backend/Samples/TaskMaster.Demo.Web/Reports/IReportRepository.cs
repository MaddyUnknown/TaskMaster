using TaskMaster.Demo.Web.Data;

namespace TaskMaster.Demo.Web.Reports;

public interface IReportRepository
{
    Task AddAsync(ReportRequestEntity entity, CancellationToken cancellationToken);

    Task<ReportRequestEntity?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid id, string reason, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportRequestEntity>> GetExpiredAsync(DateTimeOffset cutoffUtc, int maxRows, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
