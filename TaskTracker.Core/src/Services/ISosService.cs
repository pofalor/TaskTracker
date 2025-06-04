using TaskTracker.Core.src.DataResult;

namespace TaskTracker.Core.src.Services
{
    public interface ISosService
    {
        Task<IDataResult<bool>> CreateNewRole(string roleName);

        Task<IDataResult<bool>> SetToRole(string roleName, int userId);
    }
}
