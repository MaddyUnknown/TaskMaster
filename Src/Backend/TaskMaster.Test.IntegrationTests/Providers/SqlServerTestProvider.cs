using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Providers
{
    public class SqlServerTestProvider : ITestProvider
    {
        public string Name => "SqlServer";

        public DatabaseProviderEnum Provider => DatabaseProviderEnum.SqlServer;

        public string ConnectionStringName => "SqlServerTesting";

        public string? ResolveConnectionString(IConfiguration configuration)
        {
            var configured = configuration.GetConnectionString(ConnectionStringName);
            return string.IsNullOrWhiteSpace(configured) ? null : configured;
        }

        public void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
        {
            options.UseSqlServer(connectionString);
        }

        public string ResetScript => """
            DELETE FROM SystemActivities;
            DELETE FROM Jobs;
            DELETE FROM WorkerCapabilities;
            DELETE FROM Workers;
            DELETE FROM JobTypes;
            """;

        public string DeleteWorkersScript => """DELETE FROM Workers""";

        public string DeleteJobTypesScript => """DELETE FROM JobTypes""";
    }
}
