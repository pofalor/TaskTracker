using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    /// <summary>
    /// Запросы на смену статусов юзерам в рабочих пространствах(либо приглашение в рабочее пространство, либо удаление из него)
    /// </summary>
    public class UserWorkspaceStatusChangeRequest : PersistentEntity
    {
        /// <summary>
        /// Ссылка на рабочее пространство, 
        /// в рамках которого юзер меняет статус
        /// </summary>
        public WorkSpace WorkSpace { get; set; } = null!;

        public int WorkSpaceId { get; set; }

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
        public UserWorkSpaceStatus? PreviousStatus { get; set; }

        public UserWorkSpaceStatus NewStatus { get; set; }

        /// <summary>
        /// Подтвердил юзер приглашение или нет
        /// </summary>
        public UserStatusChangeType RequestStatus { get; set; }

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
