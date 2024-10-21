using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class BaseService<T, F> : IBaseService<T, F>
        where T : PersistentEntity
        where F : BaseFilter
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;

        public BaseService(ApplicationDbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IDataResult<List<T>>> GetAll()
        {
            var result = new DataResult<List<T>>();
            try
            {
                var data = await _dbContext.Set<T>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                return result.WithData(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}().{NewLine} Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseService<T, F>), nameof(GetAll), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(BaseErrorCodes.GetItemsError);
            }
        }

        public virtual IQueryable<T> FiltrationItem(IQueryable<T> dataToFilter, F filter)
        {
            try
            {
                var filtered = dataToFilter
                    .WhereIf(filter != null && filter.BeginDate.HasValue, x => filter.BeginDate.Value <= x.ObjectCreateDate)
                    .WhereIf(filter != null && filter.EndDate.HasValue, x => filter.EndDate.Value > x.ObjectCreateDate);

                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}().{NewLine} Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseService<T, F>), nameof(FiltrationItem), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return dataToFilter;
            }
        }

        public virtual async Task<IDataResult<List<T>>> GetByFilter(F filter)
        {
            var result = new DataResult<List<T>>();
            try
            {
                var dataToFilter = _dbContext.Set<T>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted);

                var filtered = FiltrationItem(dataToFilter, filter);
                var data = await filtered.ToListAsync();

                return result.WithData(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}().{NewLine} Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseService<T, F>), nameof(GetByFilter), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(BaseErrorCodes.GetItemsError);
            }
        }

        public async Task<IDataResult<T>> GetById(int id)
        {
            var result = new DataResult<T>();
            try
            {
                var item = await _dbContext.Set<T>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Id == id)
                    .FirstOrDefaultAsync();

                if (item == null) 
                    return result.WithError(BaseErrorCodes.GetItemError);

                return result.WithData(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}().{NewLine} Msg: {Message}{StackTrace}{InnerException}",
                   nameof(BaseService<T, F>), nameof(GetById), Environment.NewLine,
                   ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(BaseErrorCodes.GetItemError);
            }
        }

        public async Task<IDataResult<bool>> DeleteById(int Id)
        {
            var result = new DataResult<bool>();
            try
            {
                var item = await _dbContext.Set<T>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Id == Id)
                    .FirstOrDefaultAsync();

                if (item == null) 
                    return result.WithError(BaseErrorCodes.GetItemError);

                item.IsDeleted = true;
                await _dbContext.AddAsync(item);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}().{NewLine} Msg: {Message}{StackTrace}{InnerException}",
                   nameof(BaseService<T, F>), nameof(DeleteById), Environment.NewLine,
                   ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(BaseErrorCodes.DeleteItemError);
            }
        }

        public async Task<IDataResult<bool>> CreateOrEdit(T request)
        {
            var result = new DataResult<bool>();
            try
            {
                await _dbContext.AddAsync(request);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}().{NewLine} Msg: {Message}{StackTrace}{InnerException}",
                   nameof(BaseService<T, F>), nameof(CreateOrEdit), Environment.NewLine,
                   ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(BaseErrorCodes.CreateItemError);
            }
        }
    }
}
