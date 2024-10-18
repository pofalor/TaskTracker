using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Models.Filters;

namespace TaskTracker.Core.src.Services
{
    public interface IBaseService<T, F>
    where T : PersistentEntity
    where F : BaseFilter
    {
        Task<IDataResult<List<T>>> GetAll();
        Task<IDataResult<List<T>>> GetByFilter(F filter);
        IQueryable<T> FiltrationItem(IQueryable<T> dataToFilter, F filter);
        Task<IDataResult<T>> GetById(int id);
        Task<IDataResult<bool>> DeleteById(int Id);
        Task<IDataResult<bool>> CreateOrEdit(T request);
    }
}
