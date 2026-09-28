using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;
using SqlServerIndexBuilderExtensions = Microsoft.EntityFrameworkCore.SqlServerIndexBuilderExtensions;

namespace TaskMaster.API.Data.SqlServer
{
    public class SqlServerDbContext : ApplicationDbContext
    {
        protected override string DateTimeColumnType => "datetime2";

        public SqlServerDbContext(
            DbContextOptions<SqlServerDbContext> options,
            IEnumerable<ISaveInterceptor> saveInterceptors) : base(options, saveInterceptors)
        {
        }

        protected override void ConfigureJobQueueIndex(ModelBuilder modelBuilder)
        {
            var index = modelBuilder.Entity<Job>()
                .HasIndex(j => new { j.JobTypeId, j.Status })
                .HasDatabaseName("IX_Jobs_JobTypeId_Status");

            SqlServerIndexBuilderExtensions.IncludeProperties(index, j => j.Id);
        }

        protected override void ConfigureWorkerExpiryIndex(ModelBuilder modelBuilder)
        {
            var index = modelBuilder.Entity<Worker>()
                .HasIndex(w => new { w.Status, w.WorkerExpiresAtTimestamp })
                .HasDatabaseName("IX_Workers_Status_WorkerExpiresAtTimestamp");

            SqlServerIndexBuilderExtensions.IncludeProperties(index, w => w.Id);
        }
    }
}
