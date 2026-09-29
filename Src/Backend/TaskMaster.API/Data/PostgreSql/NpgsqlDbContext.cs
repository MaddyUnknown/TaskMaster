using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Interfaces.Data;

namespace TaskMaster.API.Data.PostgreSql
{
    public class NpgsqlDbContext : ApplicationDbContext
    {
        protected override string DateTimeColumnType => "timestamp with time zone";

        public NpgsqlDbContext(
            DbContextOptions<NpgsqlDbContext> options,
            IEnumerable<ISaveInterceptor> saveInterceptors) : base(options, saveInterceptors)
        {
        }
    }
}
