using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Entities.Abstractions;

namespace TaskMaster.API.Data
{
    public class AuditDateTimeSaveInterceptor : ISaveInterceptor
    {
        public Task OnSaveAsync(DbContext dbContext)
        {
            foreach(var entity in dbContext.ChangeTracker.Entries<BaseEntity>())
            {
                if(entity.State == EntityState.Added)
                {
                    entity.Entity.CreatedDateTime = DateTime.UtcNow;
                }
                else if(entity.State == EntityState.Modified)
                {
                    entity.Entity.ModifyDateTime = DateTime.UtcNow;
                }
            }

            return Task.CompletedTask;
        }
    }
}
