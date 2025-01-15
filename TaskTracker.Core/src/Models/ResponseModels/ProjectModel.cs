namespace TaskTracker.Core.src.Models.ResponseModels
{
    public class ProjectModel 
    {
        public int Id { get; set; }

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
        public string StartDate { get; set; } = null!;

        /// <summary>
        /// Дата завершения проекта(дедлайн)
        /// </summary>
        public string? EndDate { get; set; }

        /// <summary>
        /// Автор проекта
        /// </summary>
        public int AuthorId { get; set; }

        /// <summary>
        /// Ответственный за проект
        /// </summary>
        public int ProjectMgrId { get; set; }

        /// <summary>
        /// Рабочее пространство проекта
        /// </summary>
        public int WorkSpaceId { get; set; }
    }
}
