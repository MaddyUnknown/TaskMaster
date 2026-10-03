using Microsoft.EntityFrameworkCore;
using TaskMaster.Demo.Consumer.Data;
using TaskMaster.Demo.Consumer.Storage;

namespace TaskMaster.Demo.Consumer.Reports;

public sealed class EfReportRepository : IReportRepository
{
    private readonly DemoDbContext _context;

    public EfReportRepository(DemoDbContext context)
    {
        _context = context;
    }

    public Task<ReportRequestEntity?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ReportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task MarkRunningAsync(Guid id, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var entity = await _context.ReportRequests.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return;

        entity.Status = ReportProcessingStatus.Running;
        entity.StartedAtUtc ??= nowUtc;
        entity.ModifiedAtUtc = nowUtc;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(
        Guid id,
        StoredReport stored,
        int rowCount,
        bool truncated,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var entity = await _context.ReportRequests.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return;

        entity.Status = ReportProcessingStatus.Completed;
        entity.StartedAtUtc ??= nowUtc;
        entity.CompletedAtUtc = nowUtc;
        entity.ModifiedAtUtc = nowUtc;
        entity.FailureReason = null;

        entity.StorageKey = stored.StorageKey;
        entity.FileName = stored.FileName;
        entity.ContentType = stored.ContentType;
        entity.SizeBytes = stored.SizeBytes;
        entity.RowCount = rowCount;
        entity.Truncated = truncated;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string reason, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var entity = await _context.ReportRequests.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return;

        entity.Status = ReportProcessingStatus.Failed;
        entity.CompletedAtUtc = nowUtc;
        entity.ModifiedAtUtc = nowUtc;
        entity.FailureReason = Truncate(reason);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportRequestEntity>> GetStaleRunningAsync(
        DateTimeOffset cutoffUtc,
        int maxRows,
        CancellationToken cancellationToken) =>
        await _context.ReportRequests
            .AsNoTracking()
            .Where(e => e.Status == ReportProcessingStatus.Running && e.ModifiedAtUtc < cutoffUtc)
            .OrderBy(e => e.ModifiedAtUtc)
            .Take(maxRows)
            .ToListAsync(cancellationToken);

    private static string Truncate(string reason)
    {
        const int maxLength = 1000;
        var trimmed = (reason ?? string.Empty).Trim();

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
