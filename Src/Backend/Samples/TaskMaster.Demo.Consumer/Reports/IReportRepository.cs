using TaskMaster.Demo.Consumer.Data;
using TaskMaster.Demo.Consumer.Storage;

namespace TaskMaster.Demo.Consumer.Reports;

/// <summary>
/// Persistence for demo report rows, from the consumer's side. Implemented with plain
/// EF LINQ — no raw SQL and no provider-specific operators, so the same implementation
/// runs on SQL Server and PostgreSQL.
///
/// This process is the sole writer of the Running/Completed/Failed transitions; the web
/// tier only creates rows and deletes expired ones. No two processes ever write the same
/// column, which is why there is no concurrency token.
///
/// The entity and <c>DemoDbContext</c> are identical copies of the ones in
/// TaskMaster.Demo.Web — those two must match exactly. The method set differs by design:
/// each side exposes only what it calls.
/// </summary>
public interface IReportRepository
{
    Task<ReportRequestEntity?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Moves a queued row to Running. No-op when the row is gone.</summary>
    Task MarkRunningAsync(Guid id, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    /// <summary>Moves a row to Completed and records the stored file's metadata.</summary>
    Task MarkCompletedAsync(Guid id, StoredReport stored, int rowCount, bool truncated, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    /// <summary>Moves a row to Failed.</summary>
    Task MarkFailedAsync(Guid id, string reason, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Rows left Running with no update since the cutoff — i.e. a previous run of this
    /// worker died mid-handler. Bounded so a sweep never runs unbounded.
    /// </summary>
    Task<IReadOnlyList<ReportRequestEntity>> GetStaleRunningAsync(DateTimeOffset cutoffUtc, int maxRows, CancellationToken cancellationToken);
}
