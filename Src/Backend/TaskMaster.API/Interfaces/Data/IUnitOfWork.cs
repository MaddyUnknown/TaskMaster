namespace TaskMaster.Interfaces.Data
{
    public interface IUnitOfWork
    {
        Task<int> SaveAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
