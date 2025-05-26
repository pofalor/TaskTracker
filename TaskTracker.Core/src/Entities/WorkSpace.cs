using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    public class WorkSpace : PersistentEntity
    {
        /// <summary>
        /// Название рабочего пространства
        /// </summary>
        public string Name { get; set; } = null!;
        public WorkSpaceType WorkSpaceType { get; set; }

        /// <summary>
        /// Ссылка на управляющего компании
        /// </summary>
        public int DirectorUserId { get; set; }
        public User DirectorUser { get; set; } = null!;

        //Все поля ниже заполняются, если WorkSpaceType - Company

        /// <summary>
        /// Страна, в которой компания зарегистрирована
        /// </summary>
        public int? Country { get; set; }

        /// <summary>
        /// Дата регистрации в UTC
        /// </summary>
        public DateTime? RegistrationDate { get; set; }

        /// <summary>
        /// Юр. адрес
        /// </summary>
        public string? Address { get; set; }

        public string? INN { get; set; }

        /// <summary>
        /// Статус проверки рабочего пространства администратором
        /// Null означает, что рабочее пространство не требует проверки администратором (у личных)
        /// </summary>
        public WorkspaceReviewStatus? ReviewStatus { get; set; }

    }
}
