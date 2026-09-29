using Microsoft.EntityFrameworkCore;
using System.Threading;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.Test.IntegrationTests.Providers;

namespace TaskMaster.Test.IntegrationTests.Factories
{
    public class DatabaseFixture
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ITestProvider _provider;

        public DatabaseFixture(IApplicationDbContext dbContext, ITestProvider provider)
        {
            _dbContext = dbContext;
            _provider = provider;
        }

        public async Task InitializeAsync()
        {
            var pending = (await _dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0) return;

            await _dbContext.Database.MigrateAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public async Task ResetDatabaseAsync()
        {
            await _dbContext.Database.ExecuteSqlRawAsync(_provider.ResetScript);
        }
    }
}
