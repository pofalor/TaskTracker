using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;

namespace TaskTracker.Core.src.Services
{
    public interface IWorkSpaceService : IBaseService<WorkSpace, WorkSpaceFilter>
    {
        Task<IDataResult<List<WorkSpaceMember>>> GetMyWorkspaces(int userId);

        /// <summary>
        /// Получить приглашения юзера в воркспейсы. Т.е. куда меня как юзера пригласили
        /// </summary>
        Task<IDataResult<List<UserWorkspaceStatusChangeRequest>>> GetUserInvitations(int userId);

        /// <summary>
        /// Создать инвайт в воркспейс
        /// </summary>
        Task<IDataResult<bool>> CreateWpsInvitationRequest(UserWorkspaceStatusChangeRequest request);
    }
}
