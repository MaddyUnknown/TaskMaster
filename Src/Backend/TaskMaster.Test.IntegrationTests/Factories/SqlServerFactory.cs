using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.API.Data;

namespace TaskMaster.Test.IntegrationTests.Factories
{
    public class SqlServerFactory
    {
        private ApplicationDbContext _dbContext;

        public SqlServerFactory(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task InitializeAsync()
        {
            await _dbContext.Database.MigrateAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task ResetDatabaseAsync()
        {
            await _dbContext.Database.ExecuteSqlRawAsync("""
                DELETE FROM Jobs;
                DELETE FROM WorkerCapabilities;
                DELETE FROM Workers;
                DELETE FROM JobTypes;
            """);
        }
    }
}
