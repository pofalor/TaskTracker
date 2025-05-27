using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    /// <summary>
    /// Запросы на смену статусов юзерам в рабочих пространствах(либо приглашение в рабочее пространство, либо удаление из него)
    /// </summary>
    public class WorkspaceInvite : PersistentEntity
    {
        /// <summary>
        /// Ссылка на рабочее пространство, 
        /// в рамках которого юзер меняет статус
        /// </summary>
        public Workspace Workspace { get; set; } = null!;

        public int WorkspaceId { get; set; }

        /// <summary>
        /// Юзер, которого приглашают или удаляют из рабочего пространства
        /// </summary>
        public User User { get; set; } = null!;
        public int UserId { get; set; }

        /// <summary>
        /// Юзер, который приглашает или удаляет из рабочего пространства
        /// </summary>
        public User Inviter { get; set; } = null!;
        public int InviterId { get; set; }

        /// <summary>
        /// Дата, когда был создан запрос в UTC
        /// </summary>
        public DateTime Date { get; set; } 
        public UserWorkspaceStatus? PreviousStatus { get; set; }

        public UserWorkspaceStatus NewStatus { get; set; }

        /// <summary>
        /// Подтвердил юзер приглашение или нет
        /// </summary>
        public InviteStatus RequestStatus { get; set; }

        /// <summary>
        /// Просмотрел ли бек жоб 
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// Скрыл ли юзер с фронта этот запрос(пока не используется, функционал на будущее)
        /// </summary>
        public bool IsHidden { get; set; }
    }
}
