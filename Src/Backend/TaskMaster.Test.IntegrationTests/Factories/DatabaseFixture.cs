using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.Test.IntegrationTests.Providers;

namespace TaskMaster.Test.IntegrationTests.Factories
{
    /// <summary>
    /// Applies the engine's migrations to an empty database and empties it between tests.
    /// Engine specific work (which migrations set exists, how to truncate) is delegated to
    /// the <see cref="ITestProvider"/>.
    /// </summary>
    public class DatabaseFixture
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ITestProvider _provider;

        public DatabaseFixture(IApplicationDbContext dbContext, ITestProvider provider)
        {
            _dbContext = dbContext;
            _provider = provider;
        }

        public Task InitializeAsync() => _dbContext.Database.MigrateAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        public async Task ResetDatabaseAsync()
        {
            await _dbContext.Database.ExecuteSqlRawAsync(_provider.ResetScript);
        }
    }
}
