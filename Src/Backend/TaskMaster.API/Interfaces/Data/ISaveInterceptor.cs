using Microsoft.EntityFrameworkCore;

namespace TaskMaster.API.Interfaces.Data
{
    public interface ISaveInterceptor
    {
        Task OnSaveAsync(DbContext dbContext);
    }
}
