using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Entities
{
    public class TimeTracking : PersistentEntity
    {
        /// <summary>
        /// Затраченное время
        /// </summary>
        public TimeSpan TimeSpent { get; set; }

        /// <summary>
        /// Дата начала работы
        /// </summary>
        public DateTime DateBegin { get; set; }

        /// <summary>
        /// Комментарий к списанным часам
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Статус автоматического трекинга времени. 
        /// Если не задано - значит трекинг выполнен руками
        /// </summary>
        public AutoTrackTimeStatus? AutoTrackStatus { get; set; }

        /// <summary>
        /// Юзер, который списал часы
        /// </summary>
        public User User { get; set; } = null!;
        public int UserId { get; set; }

        /// <summary>
        /// Задача, по которой списали часы
        /// </summary>
        public Issue Issue { get; set; } = null!;
        public int IssueId { get; set; }
    }
}
