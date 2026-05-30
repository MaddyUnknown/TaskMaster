using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.Entities;

namespace TaskMaster.Data
{
    public class ApplicationDbContext : DbContext
    {
        private IOptions<WorkerConfig> _workerConfigOption;
        private IEnumerable<ISaveInterceptor> _saveInterceptors;

        public DbSet<Worker> Workers { get; set; }
        public DbSet<WorkerCapabality> WorkerCapabalities { get; set; }
        public DbSet<Job> Jobs { get; set; }


        public ApplicationDbContext(DbContextOptions options, IEnumerable<ISaveInterceptor> saveInterceptors, IOptions<WorkerConfig> workerConfigOption) : base(options)
        {
            _saveInterceptors = saveInterceptors;
            _workerConfigOption = workerConfigOption;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Worker>()
                .HasMany(w => w.AssignedJobs)
                .WithOne()
                .HasForeignKey(j => j.AssignedWorkerId);

            modelBuilder.Entity<Worker>()
                .HasMany(w => w.JobTypeCapabilities)
                .WithOne()
                .HasForeignKey(j => j.WorkerId);

            var workerExpiryIntervalSec = _workerConfigOption.Value.WorkerExpiryIntervalSeconds;
            modelBuilder.Entity<Worker>().Property<DateTime>("WorkerExpiresAtTimestamp").HasColumnType("datetime2").HasDefaultValueSql($"DATEADD(SECOND, {workerExpiryIntervalSec}, SYSDATETIME())");
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
