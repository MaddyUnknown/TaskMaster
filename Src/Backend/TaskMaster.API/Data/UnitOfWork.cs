using Microsoft.EntityFrameworkCore.Storage;
using TaskMaster.Interfaces.Data;

namespace TaskMaster.Data
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private ApplicationDbContext _context;
        private IDbContextTransaction? _dbTransaction;
        private bool _isDisposed = false;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_dbTransaction != null) throw new Exception();

            _dbTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_dbTransaction == null) throw new Exception();

            await _dbTransaction.CommitAsync();
            _dbTransaction?.Dispose();
            _dbTransaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_dbTransaction == null) throw new Exception();

            await _dbTransaction.RollbackAsync();
            _dbTransaction?.Dispose();
            _dbTransaction = null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _dbTransaction?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
