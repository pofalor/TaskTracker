using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Resources.ErrorCodes;

namespace TaskTracker.Core.src.ErrorCodes
{
    public enum IssueErrorCodes
    {
        /// <summary>
        /// Не удалось получить информацию о задачах
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(CannotGetIssues))]
        CannotGetIssues = ProjectErrorCodes.CannotGetProjects + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Юзер не является членом рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(UserNotMemberWsp))]
        UserNotMemberWsp = CannotGetIssues + 1,

        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(ProjectNotSet))]
        ProjectNotSet = UserNotMemberWsp + 1,

        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(AuthorNotSet))]
        AuthorNotSet = ProjectNotSet + 1,

        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(EmptyName))]
        EmptyName = AuthorNotSet + 1,


        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(CannotCreateIssue))]
        CannotCreateIssue = EmptyName + 1,

        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(IssueNotSet))]
        IssueNotSet = CannotCreateIssue + 1,

        [ErrorMessage(typeof(IssueErrorCodeResources), nameof(CannotCreateTimeTrack))]
        CannotCreateTimeTrack = IssueNotSet + 1,
    }
}
