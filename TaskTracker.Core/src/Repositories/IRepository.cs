using System.Linq.Expressions;
using TaskTracker.Core.src.DataAccess.BaseClasses;

namespace TaskTracker.Core.src.Repositories
{
    public interface IRepository<T> where T : PersistentEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task SaveChangesAsync();
    }
}
