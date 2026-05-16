using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class CreateOrEditIssuePR : BasePostRequest
    {
        public int Id { get; set; }
        /// <summary>
        /// Название задачи
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Описание задачи
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public IssueType Type { get; set; }

        public IssueStatus Status { get; set; }

        public IssuePriority Priority { get; set; }

        /// <summary>
        /// Оценка задачи
        /// </summary>
        public string? Estimate { get; set; } = string.Empty;

        /// <summary>
        /// Порядковый номер задачи внутри проекта
        /// </summary>
        public int Index { get; set; }

        public int? ParentId { get; set; }

        public int? AuthorId { get; set; }

        /// <summary>
        /// Исполнитель
        /// </summary>
        public int? AssigneeId { get; set; }

        public int ProjectId { get; set; }
    }
}
