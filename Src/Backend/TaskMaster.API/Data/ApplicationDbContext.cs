using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;

namespace TaskMaster.API.Data
{
    public abstract class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly IEnumerable<ISaveInterceptor> _saveInterceptors;

        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerCapability> WorkerCapabilities { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobType> JobTypes { get; set; }
        public DbSet<SystemActivity> SystemActivities { get; set; }

        protected ApplicationDbContext(
            DbContextOptions options,
            IEnumerable<ISaveInterceptor> saveInterceptors) : base(options)
        {
            _saveInterceptors = saveInterceptors ?? Array.Empty<ISaveInterceptor>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureJobEntity(modelBuilder);
            ConfigureJobTypeEntity(modelBuilder);
            ConfigureWorkerEntity(modelBuilder);
            ConfigureWorkerCapabilityEntity(modelBuilder);
            ConfigureSystemActivityEntity(modelBuilder);

            ConfigureJobQueueIndex(modelBuilder);
            ConfigureWorkerExpiryIndex(modelBuilder);
            ConfigureDateTimeColumns(modelBuilder);
        }

        private static void ConfigureJobEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>()
                .HasIndex(j => new { j.JobPublicId })
                .IsUnique();

            modelBuilder.Entity<Job>()
                .HasOne(j => j.JobType)
                .WithMany()
                .HasForeignKey(j => j.JobTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Job>()
                .HasOne(j => j.AssignedWorker)
                .WithMany()
                .HasForeignKey(j => j.AssignedWorkerId);
        }

        private static void ConfigureJobTypeEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobType>()
                .HasIndex(t => new { t.Name, t.Version })
                .IsUnique();
        }

        private void ConfigureWorkerEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Worker>()
                .HasIndex(w => new { w.WorkerName })
                .IsUnique();

            modelBuilder.Entity<Worker>()
                .HasIndex(w => new { w.WorkerPublicId })
                .IsUnique();
        }

        private static void ConfigureWorkerCapabilityEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkerCapability>()
                .HasIndex(wc => new { wc.WorkerId, wc.JobTypeId })
                .IsUnique();

            modelBuilder.Entity<WorkerCapability>()
                .HasOne(wc => wc.JobType)
                .WithMany()
                .HasForeignKey(wc => wc.JobTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigureSystemActivityEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemActivity>()
                .HasIndex(a => a.CreatedDateTime);
        }

        protected virtual void ConfigureJobQueueIndex(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>()
                .HasIndex(j => new { j.JobTypeId, j.Status })
                .HasDatabaseName("IX_Jobs_JobTypeId_Status");
        }

        protected virtual void ConfigureWorkerExpiryIndex(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Worker>()
                .HasIndex(w => new { w.Status, w.WorkerExpiresAtTimestamp })
                .HasDatabaseName("IX_Workers_Status_WorkerExpiresAtTimestamp");
        }

        protected virtual void ConfigureDateTimeColumns(ModelBuilder modelBuilder)
        {
            ConfigureAuditTimestamps(modelBuilder.Entity<Job>());
            ConfigureAuditTimestamps(modelBuilder.Entity<JobType>());
            ConfigureAuditTimestamps(modelBuilder.Entity<Worker>());
            ConfigureAuditTimestamps(modelBuilder.Entity<WorkerCapability>());
            ConfigureAuditTimestamps(modelBuilder.Entity<SystemActivity>());

            modelBuilder.Entity<Worker>()
                .Property(w => w.WorkerExpiresAtTimestamp)
                .HasColumnType(DateTimeColumnType);

            modelBuilder.Entity<Worker>()
                .Property(w => w.LastHeartBeatTimestamp)
                .HasColumnType(DateTimeColumnType);
        }

        private void ConfigureAuditTimestamps<TEntity>(
            EntityTypeBuilder<TEntity> builder) where TEntity : Entities.Abstractions.BaseEntity
        {
            builder.Property(e => e.CreatedDateTime).HasColumnType(DateTimeColumnType);
            builder.Property(e => e.ModifyDateTime).HasColumnType(DateTimeColumnType);
        }

        protected abstract string DateTimeColumnType { get; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var interceptor in _saveInterceptors)
            {
                await interceptor.OnSaveAsync(this);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
