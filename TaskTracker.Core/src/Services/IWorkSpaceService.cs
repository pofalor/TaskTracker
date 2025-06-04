using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services
{
    public interface IWorkspaceService
    {
        Task<IDataResult<List<WorkspaceMember>>> GetMyWorkspaces(int userId);

        /// <summary>
        /// Получить приглашения юзера в воркспейсы. Т.е. куда меня как юзера пригласили
        /// </summary>
        Task<IDataResult<List<WorkspaceInvite>>> GetUserInvitations(int userId);

        /// <summary>
        /// Создать инвайт в воркспейс
        /// </summary>
        Task<IDataResult<bool>> CreateWpsInvitationRequest(WorkspaceInvite request);

        /// <summary>
        /// Поиск пользователей для создания инвайта
        /// </summary>
        Task<IDataResult<List<User>>> SearchUsersForInvite(SearchUserForInvitePR searchUser);

        /// <summary>
        /// Проверить является ли юзер членом рабочего пространства
        /// </summary>
        Task<bool> IsWorkspaceMember(int userId, int workspaceId);

        /// <summary>
        /// Проверить является ли юзер владельцем организации
        /// </summary>
        Task<bool> IsWorkspaceOwner(int userId, int workspaceId);

        Task<IDataResult<List<WorkspaceInvite>>> GetUserCreatedInvites(int userId, int workspaceId);

        /// <summary>
        /// Принять или отклонить запрос на вступление в рабочее пространство
        /// </summary>
        Task<IDataResult<bool>> AcceptInvitationRequest(AcceptInvitePR request);

        Task<IDataResult<bool>> CreateOrEdit(Workspace request);

        /// <summary>
        /// Получить рабочие пространства для проверки администратором
        /// </summary>
        Task<IDataResult<List<Workspace>>> GetWorkspacesForCheck(int adminId);

        Task<IDataResult<bool>> ChangeWorkspaceReviewStatus(Workspace workspace);
    }
}
