using Microsoft.EntityFrameworkCore;

namespace TaskMaster.Demo.Web.Data;

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

        entity.ToTable("ReportRequests");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Title).HasMaxLength(80).IsRequired();
        entity.Property(e => e.SubmittedBy).HasMaxLength(200);
        entity.Property(e => e.FailureReason).HasMaxLength(1000);
        entity.Property(e => e.FileName).HasMaxLength(260);
        entity.Property(e => e.StorageKey).HasMaxLength(400);
        entity.Property(e => e.ContentType).HasMaxLength(100);

        entity.Property(e => e.Status).HasConversion<int>();

        // Retention scans by submit time.
        entity.HasIndex(e => e.SubmittedAtUtc);
    }
}
