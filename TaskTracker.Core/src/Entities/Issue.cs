using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    public class Issue : PersistentEntity
    {
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
        public TimeSpan? Estimate { get; set; }

        /// <summary>
        /// Порядковый номер задачи внутри проекта
        /// </summary>
        public int Index { get; set; }

        public Issue? Parent { get; set; }
        public int? ParentId { get; set; }
        public ICollection<Issue> Children { get; set; } = new List<Issue>();

        public User Author { get; set; } = null!;
        public int AuthorId { get; set; }

        /// <summary>
        /// Исполнитель
        /// </summary>
        public User? Assignee { get; set; } 
        public int? AssigneeId { get; set; }

        public Project Project { get; set; } = null!;
        public int ProjectId { get; set; }
    }
}
