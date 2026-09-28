using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Configs;
using TaskMaster.API.Interfaces.Data;

namespace TaskMaster.API.Data
{
    public static class PersistenceMigrationExtensions
    {
        public static async Task ApplyPendingMigrationsAsync(this IHost host, ILogger logger, CancellationToken cancellationToken = default)
        {
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var settings = configuration.GetSection(PersistenceConfig.SectionName).Get<PersistenceConfig>() ?? new PersistenceConfig();

            if (!settings.AutoMigrate)
            {
                logger.LogInformation("Automatic migration is disabled (Database:AutoMigrate=false); skipping. Pending migrations must be applied out of band.");
                return;
            }

            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Database schema is already up to date; no migrations to apply.");
                return;
            }

            logger.LogInformation("Applying {PendingCount} pending database migration(s): {PendingMigrations}", pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Database migrations applied successfully.");
        }
    }
}
