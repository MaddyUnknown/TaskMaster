using TaskMaster.Demo.Web.Data;

namespace TaskMaster.Demo.Web.Reports;

/// <summary>
/// Persistence for demo report rows. Implemented with plain EF LINQ — no raw SQL and
/// no provider-specific operators, so the same implementation runs on SQL Server and
/// PostgreSQL.
///
/// Ownership is deliberately split so the two processes never write the same column:
/// the web tier creates rows and deletes expired ones; the consumer owns the
/// Running/Completed/Failed transitions. That is why no concurrency token is needed.
///
/// TaskMaster.Demo.Consumer holds a copy of this interface, but the two sides are not
/// interchangeable: each exposes only what its own process calls. Only the entity and
/// <c>DemoDbContext</c> copies have to match exactly.
/// </summary>
public interface IReportRepository
{
    /// <summary>Inserts a new row in <see cref="ReportProcessingStatus.Queued"/>.</summary>
    Task AddAsync(ReportRequestEntity entity, CancellationToken cancellationToken);

    Task<ReportRequestEntity?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Moves a row to Failed. Also used to unstick rows abandoned by a crashed consumer.</summary>
    Task MarkFailedAsync(Guid id, string reason, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    /// <summary>Rows submitted before the cutoff, oldest first. Bounded so a sweep never runs unbounded.</summary>
    Task<IReadOnlyList<ReportRequestEntity>> GetExpiredAsync(DateTimeOffset cutoffUtc, int maxRows, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
