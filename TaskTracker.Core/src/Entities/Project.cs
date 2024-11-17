using TaskTracker.Core.src.DataAccess.BaseClasses;

namespace TaskTracker.Core.src.Entities
{
    public class Project : PersistentEntity
    {
        /// <summary>
        /// Название проекта
        /// </summary>
        public string Name { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Короткое имя проекта(или код проекта) обычно 2-4 символа
        /// </summary>
        public string Code { get; set; } = null!;

        /// <summary>
        /// Дата старта проекта
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Дата завершения проекта(дедлайн)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Автор проекта
        /// </summary>
        public User Author { get; set; } = null!;
        public int AuthorId { get; set; }
        
        /// <summary>
        /// Ответственный за проект
        /// </summary>
        public User ProjectMgr { get; set; } = null!;
        public int ProjectMgrId { get; set; }

        /// <summary>
        /// Рабочее пространство проекта
        /// </summary>
        public WorkSpace WorkSpace { get; set; } = null!;
        public int WorkSpaceId { get; set; }
    }
}
