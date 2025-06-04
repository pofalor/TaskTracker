using System.Globalization;
using TaskTracker.Core.src.Constants;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class AutoTimeTrackPR : BasePostRequest
    {
        public int? Id {  get; set; }

        /// <summary>
        /// Затраченное время
        /// </summary>
        public string TimeSpent { get; set; } = null!;

        /// <summary>
        /// Дата начала работы
        /// </summary>
        public string DateBegin { get; set; } = string.Empty;

        /// <summary>
        /// Комментарий к списанным часам
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Юзер, который списал часы
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Задача, по которой списали часы
        /// </summary>
        public int IssueId { get; set; }

        public DateTime GetBeginDate()
        {
            return DateBegin.IsEmpty() 
                ? DateTime.UtcNow - TimeSpent.ConvertToTimespan()
                : DateTime.ParseExact(DateBegin, DateFormatConstants.IsoString, CultureInfo.InvariantCulture);
        }
    }
}
