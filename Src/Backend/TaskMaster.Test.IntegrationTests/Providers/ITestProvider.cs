using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Providers
{
    public interface ITestProvider
    {
        string Name { get; }

        DatabaseProviderEnum Provider { get; }

        string ConnectionStringName { get; }

        string? ResolveConnectionString(IConfiguration configuration);

        void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString);

        string ResetScript { get; }

        string DeleteWorkersScript { get; }

        string DeleteJobTypesScript { get; }
    }
}
