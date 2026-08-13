using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;

namespace TaskMaster.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        private IEnumerable<ISaveInterceptor> _saveInterceptors;

        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerCapability> WorkerCapabilities { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobType> JobTypes { get; set; }
        public DbSet<SystemActivity> SystemActivities { get; set; }

        public ApplicationDbContext(DbContextOptions options, IEnumerable<ISaveInterceptor> saveInterceptors) : base(options)
        {
            _saveInterceptors = saveInterceptors;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Job Setup
            modelBuilder.Entity<Job>()
                .HasIndex(j => new { j.JobPublicId })
                .IsUnique();

            modelBuilder.Entity<Job>()
                .HasIndex(j => new { j.JobTypeId, j.Status })
                .HasDatabaseName("IX_Jobs_JobTypeId_Status")
                .IncludeProperties(j => j.Id);

            modelBuilder.Entity<Job>()
                .HasOne(j => j.JobType)
                .WithMany()
                .HasForeignKey(j => j.JobTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Job>()
                .HasOne(j => j.AssignedWorker)
                .WithMany()
                .HasForeignKey(j => j.AssignedWorkerId);

            // JobType Setup
            modelBuilder.Entity<JobType>()
                .HasIndex(t => new { t.Name, t.Version })
                .IsUnique();

            // Worker Setup
            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.WorkerName)
                .IsUnique();

            modelBuilder.Entity<Worker>()
                .HasIndex(w => new { w.WorkerPublicId })
                .IsUnique();

            modelBuilder.Entity<Worker>()
                .HasIndex(w => new { w.Status, w.WorkerExpiresAtTimestamp })
                .HasDatabaseName("IX_Workers_Status_WorkerExpiresAtTimestamp")
                .IncludeProperties(w => w.Id);

            modelBuilder.Entity<Worker>()
                .Property(w => w.WorkerExpiresAtTimestamp)
                .HasColumnType("datetime2");

            modelBuilder.Entity<Worker>()
                .Property(w => w.LastHeartBeatTimestamp)
                .HasColumnType("datetime2");

            // WorkerCapability Setup
            modelBuilder.Entity<WorkerCapability>()
                .HasIndex(wc => new { wc.WorkerId, wc.JobTypeId })
                .IsUnique();

            modelBuilder.Entity<WorkerCapability>()
                .HasOne(wc => wc.JobType)
                .WithMany()
                .HasForeignKey(wc => wc.JobTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // SystemActivity Setup
            modelBuilder.Entity<SystemActivity>()
                .HasIndex(a => a.CreatedDateTime);
        }

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
