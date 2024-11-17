using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    public class WorkSpaceMember : PersistentEntity
    {
        /// <summary>
        /// Роль юзера в команде
        /// </summary>
        public UserTeamRole TeamRole { get; set; }
         
        public UserWorkSpaceStatus UserStatus { get; set; }

        public User User { get; set; } = null!;
        public int UserId { get; set; }

        public WorkSpace WorkSpace { get; set; } = null!;
        public int WorkSpaceId { get; set; }
    }
}
