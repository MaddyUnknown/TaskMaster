using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Data
{
    public interface IApplicationDbContext
    {
        DbSet<Worker> Workers { get; }
        DbSet<WorkerCapability> WorkerCapabilities { get; }
        DbSet<Job> Jobs { get; }
        DbSet<JobType> JobTypes { get; }
        DbSet<SystemActivity> SystemActivities { get; }
        DatabaseFacade Database { get; }
        
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

        void AddRange(params object[] entities);

        EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
