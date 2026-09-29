using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Demo.Web.Data;

/// <summary>
/// Demo-owned persistence. Deliberately tiny: one table holding the state the web UI
/// polls. All job persistence and lifecycle still belong to TaskMaster — this context
/// never sees a TaskMaster job.
///
/// TaskMaster.Demo.Consumer holds an identical copy of this context. The table name,
/// column names and types are pinned explicitly rather than derived from EF naming
/// conventions, so the two copies can never map to different shapes.
/// </summary>
public sealed class DemoDbContext : DbContext
{
    public DemoDbContext(DbContextOptions<DemoDbContext> options) : base(options)
    {
    }

    public DbSet<ReportRequestEntity> ReportRequests => Set<ReportRequestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<ReportRequestEntity>();

        // Explicit table name: the default convention would derive
        // "ReportRequestEntities" from the CLR type name.
        entity.ToTable("ReportRequests");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Title).HasMaxLength(80).IsRequired();
        entity.Property(e => e.SubmittedBy).HasMaxLength(200);
        entity.Property(e => e.FailureReason).HasMaxLength(1000);
        entity.Property(e => e.FileName).HasMaxLength(260);
        entity.Property(e => e.StorageKey).HasMaxLength(400);
        entity.Property(e => e.ContentType).HasMaxLength(100);

        // Stored as int on both engines so the column type never depends on how a
        // provider chooses to map enums.
        entity.Property(e => e.Status).HasConversion<int>();

        // Retention scans by submit time.
        entity.HasIndex(e => e.SubmittedAtUtc);
    }
}
