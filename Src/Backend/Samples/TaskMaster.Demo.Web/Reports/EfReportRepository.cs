using Microsoft.EntityFrameworkCore;
using TaskMaster.Demo.Web.Data;

namespace TaskMaster.Demo.Web.Reports;

public sealed class EfReportRepository : IReportRepository
{
    private readonly DemoDbContext _context;

    public EfReportRepository(DemoDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ReportRequestEntity entity, CancellationToken cancellationToken)
    {
        _context.ReportRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<ReportRequestEntity?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ReportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

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

    public async Task<IReadOnlyList<ReportRequestEntity>> GetExpiredAsync(
        DateTimeOffset cutoffUtc,
        int maxRows,
        CancellationToken cancellationToken) =>
        await _context.ReportRequests
            .AsNoTracking()
            .Where(e => e.SubmittedAtUtc < cutoffUtc)
            .OrderBy(e => e.SubmittedAtUtc)
            .Take(maxRows)
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _context.ReportRequests.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return;

        _context.ReportRequests.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Matches the column width declared in <see cref="DemoDbContext"/>.</summary>
    private static string Truncate(string reason)
    {
        const int maxLength = 1000;
        var trimmed = (reason ?? string.Empty).Trim();

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
