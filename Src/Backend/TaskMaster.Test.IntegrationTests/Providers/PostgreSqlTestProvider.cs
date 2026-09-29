using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Providers
{
    public class PostgreSqlTestProvider : ITestProvider
    {
        public string Name => "PostgreSql";

        public DatabaseProviderEnum Provider => DatabaseProviderEnum.PostgreSql;

        public string ConnectionStringName => "PostgreSqlTesting";

        public string? ResolveConnectionString(IConfiguration configuration)
        {
            var configured = configuration.GetConnectionString(ConnectionStringName);
            return string.IsNullOrWhiteSpace(configured) ? null : configured;
        }

        public void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
        {
            options.UseNpgsql(connectionString);
        }

        public string ResetScript => """
            TRUNCATE TABLE "SystemActivities", "Jobs", "WorkerCapabilities", "Workers", "JobTypes"
            RESTART IDENTITY CASCADE;
            """;

        public string DeleteWorkersScript => """DELETE FROM "Workers" """.Trim();

        public string DeleteJobTypesScript => """DELETE FROM "JobTypes" """.Trim();
    }
}
