using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    public class IssueStatusHistory : PersistentEntity
    {
        public IssueStatus? OldStatus { get; set; }
        public IssueStatus NewStatus { get; set; }
        /// <summary>
        /// Когда был изменён статус. Хранится в UTC.
        /// </summary>
        public DateTime ChangedAt { get; set; }
        public User Issue { get; set; } = null!;
        public int IssueId { get; set; }

        /// <summary>
        /// Кто изменил статус
        /// </summary>
        public User ChangedByUser { get; set; } = null!;
        public int ChangedByUserId { get; set; }
    }
}
