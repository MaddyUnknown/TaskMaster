using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Providers
{
    /// <summary>
    /// Describes one database engine the integration suite can run against. The same test
    /// fixtures are instantiated once per implementation, so every integration test is
    /// provider-parametric by construction.
    /// </summary>
    public interface ITestProvider
    {
        string Name { get; }

        DatabaseProviderEnum Provider { get; }

        /// <summary>Configuration key holding the connection string for this engine.</summary>
        string ConnectionStringName { get; }

        /// <summary>Returns null when this engine is not configured on the current machine.</summary>
        string? ResolveConnectionString(IConfiguration configuration);

        void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString);

        /// <summary>
        /// Vendor specific statement that empties every table between tests. Must leave the
        /// schema in place and reset identity columns.
        /// </summary>
        string ResetScript { get; }

        /// <summary>Statement that deletes a referenced row, used to assert FK enforcement.</summary>
        string DeleteWorkersScript { get; }

        /// <summary>Statement that deletes a referenced row, used to assert FK enforcement.</summary>
        string DeleteJobTypesScript { get; }
    }
}
