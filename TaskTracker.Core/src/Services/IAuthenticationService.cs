using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Зарегистрировать нового пользователя в системе
        /// </summary>
        Task<IDataResult<bool>> RegisterNewUser(CreateUserPostRequest user);

        /// <summary>
        /// Выполнить вход, получить токен для вызова методов контроллеров
        /// </summary>
        Task<IDataResult<AuthorizationModel>> Authenticate(AuthenticatePostRequest user);
    }
}
