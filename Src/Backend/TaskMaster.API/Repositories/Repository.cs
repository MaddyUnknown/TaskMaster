using TaskMaster.Data;
using TaskMaster.Entities.Abstractions;
using TaskMaster.Interfaces.Repositories;

namespace TaskMaster.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
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
    }
}
