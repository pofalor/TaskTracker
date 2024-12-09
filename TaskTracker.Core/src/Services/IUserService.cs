using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services
{
    public interface IUserService : IBaseService<User, BaseFilter>
    {
        /// <summary>
        /// Получить юзера по айди
        /// </summary>
        Task<IDataResult<UserModel>> GetUserById(int id);
    }
}
