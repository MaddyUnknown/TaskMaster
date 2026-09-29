using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskMaster.API.Data.PostgreSql
{
    public class NpgsqlDbContextFactory : IDesignTimeDbContextFactory<NpgsqlDbContext>
    {
        public NpgsqlDbContext CreateDbContext(string[] args)
        {
            var configuration = DesignTimeConfiguration.Load();
            var connectionString = configuration.GetConnectionString(
                PersistenceServiceCollectionExtensions.DefaultConnectionStringName)
                ?? "Host=localhost;Database=TaskMaster;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<NpgsqlDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new NpgsqlDbContext(options, new[] { new AuditDateTimeSaveInterceptor() });
        }
    }
}
