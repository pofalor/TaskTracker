using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.ResponseModels
{
    public class TimeTrackingModel
    {
        public int Id { get; set; }
        /// <summary>
        /// Затраченное время
        /// </summary>
        public string TimeSpent { get; set; } = string.Empty;

        /// <summary>
        /// Дата начала работы
        /// </summary>
        public string DateBegin { get; set; } = null!;

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
        public int UserId { get; set; }

        public int IssueId { get; set; }
    }
}
