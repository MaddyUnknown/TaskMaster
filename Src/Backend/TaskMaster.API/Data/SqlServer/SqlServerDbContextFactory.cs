using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskMaster.API.Data.SqlServer
{
    public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext>
    {
        public SqlServerDbContext CreateDbContext(string[] args)
        {
            var configuration = DesignTimeConfiguration.Load();
            var connectionString = configuration.GetConnectionString(
                PersistenceServiceCollectionExtensions.DefaultConnectionStringName)
                ?? "Server=(localdb)\\mssqllocaldb;Database=TaskMaster;Trusted_Connection=True;";

            var options = new DbContextOptionsBuilder<SqlServerDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new SqlServerDbContext(options, new[] { new AuditDateTimeSaveInterceptor() });
        }
    }
}
