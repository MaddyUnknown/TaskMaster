using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;

namespace TaskMaster.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        private IOptions<WorkerConfig> _workerConfigOption;
        private IEnumerable<ISaveInterceptor> _saveInterceptors;

        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerCapability> WorkerCapabilities { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobType> JobTypes { get; set; }

        public ApplicationDbContext(DbContextOptions options, IEnumerable<ISaveInterceptor> saveInterceptors, IOptions<WorkerConfig> workerConfigOption) : base(options)
        {
            _saveInterceptors = saveInterceptors;
            _workerConfigOption = workerConfigOption;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Job Setup
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

            var workerExpiryIntervalSec = _workerConfigOption.Value.WorkerExpiryIntervalSeconds;
            modelBuilder.Entity<Worker>()
                .Property(w => w.WorkerExpiresAtTimestamp)
                .HasColumnType("datetime2")
                .HasDefaultValueSql($"DATEADD(SECOND, {workerExpiryIntervalSec}, SYSDATETIME())");

            modelBuilder.Entity<Worker>()
                .Property(w => w.LastHeartBeatTimestamp)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            // WorkerCapability Setup
            modelBuilder.Entity<WorkerCapability>()
                .HasIndex(wc => new { wc.WorkerId, wc.JobTypeId })
                .IsUnique();

            modelBuilder.Entity<WorkerCapability>()
                .HasOne(wc => wc.JobType)
                .WithMany()
                .HasForeignKey(wc => wc.JobTypeId)
                .OnDelete(DeleteBehavior.Restrict);
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
