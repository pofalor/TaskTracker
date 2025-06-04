using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    public class WorkspaceMember : PersistentEntity
    {
        /// <summary>
        /// Роль юзера в команде
        /// </summary>
        public UserTeamRole TeamRole { get; set; }
         
        public UserWorkspaceStatus UserStatus { get; set; }

        public User User { get; set; } = null!;
        public int UserId { get; set; }

        public Workspace Workspace { get; set; } = null!;
        public int WorkspaceId { get; set; }
    }
}
