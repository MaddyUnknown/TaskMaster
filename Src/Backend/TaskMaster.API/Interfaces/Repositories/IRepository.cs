namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IRepository<T>
    {
        void Add(T entity);
        void Update(T entity);
        Task<T?> GetByIdAsync(long id);
        Task<IEnumerable<T>> GetAllAsync();
    }
}
