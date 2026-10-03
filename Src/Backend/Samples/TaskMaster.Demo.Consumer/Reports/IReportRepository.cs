using TaskMaster.Demo.Consumer.Data;
using TaskMaster.Demo.Consumer.Storage;

namespace TaskMaster.Demo.Consumer.Reports;

public interface IReportRepository
{
    Task<ReportRequestEntity?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task MarkRunningAsync(Guid id, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    Task MarkCompletedAsync(Guid id, StoredReport stored, int rowCount, bool truncated, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid id, string reason, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportRequestEntity>> GetStaleRunningAsync(DateTimeOffset cutoffUtc, int maxRows, CancellationToken cancellationToken);
}
