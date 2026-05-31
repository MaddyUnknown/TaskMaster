namespace TaskMaster.API.Interfaces.Data
{
    public interface IUnitOfWork
    {
        Task<int> SaveAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
