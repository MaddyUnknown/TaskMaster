using Microsoft.EntityFrameworkCore;
using TaskMaster.API.Entities.Abstractions;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Data;


namespace TaskMaster.API.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private IApplicationDbContext _context;

        public Repository(IApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(T entity)
        {
            _context.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            //Nothing to be done for ef core as data is traced.
        }

        public async Task<T?> GetByIdAsync(long id) => await _context.Set<T>().FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();
    }
}
