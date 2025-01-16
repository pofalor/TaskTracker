using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.ResponseModels
{
    public class UserWspStatusChangeModel
    {
        /// <summary>
        /// Юзер, которого приглашают или удаляют из рабочего пространства
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Юзер, который приглашает или удаляет из рабочего пространства
        /// </summary>
        public int RequestCreatorId { get; set; }

        /// <summary>
        /// Дата, когда был создан запрос в UTC
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// Подтвердил юзер приглашение или нет
        /// </summary>
        public UserStatusChangeType RequestStatus { get; set; }

        /// <summary>
        /// Скрыл ли юзер с фронта этот запрос(пока не используется, функционал на будущее)
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Имя рабочего пространства, куда приглашают юзера
        /// </summary>
        public string WorkSpaceName { get; set; } = null!;

        /// <summary>
        /// Фамилия, имя либо никнейм того, кто приглашает в воркспейс
        /// </summary>
        public string InviterName { get; set; } = null!;

        /// <summary>
        /// Эмейл того, кто приглашает в воркспейс
        /// </summary>
        public string InviterEmail { get; set; } = null!;

        /// <summary>
        /// Фамилия, имя либо никнейм директора воркспейса
        /// </summary>
        public string DirectorWspName { get; set; } = null!;

        /// <summary>
        /// Фамилия, имя либо никнейм юзера, которого приглашают
        /// </summary>
        public string UserName { get; set; } = null!;

        /// <summary>
        /// Эмейл юзера, которого приглашают
        /// </summary>
        public string UserEmail { get; set; } = null!;
    }
}
