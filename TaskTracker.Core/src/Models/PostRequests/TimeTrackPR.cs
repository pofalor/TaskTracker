using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class TimeTrackPR : BasePostRequest
    {
        /// <summary>
        /// Затраченное время
        /// </summary>
        public string TimeSpent { get; set; } = null!;

        /// <summary>
        /// Дата начала работы
        /// </summary>
        public string DateBegin { get; set; } = DateTime.UtcNow.ToString(DateFormatConstants.FrontInputFormat);

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

        public TimeSpan GetTimeSpan()
        {
            var values = TimeSpent.Split(' ');
            var day = values[0].Replace("h", "");
            var hour = values[1].Replace("m", "");
            var minute = values[2].Replace("s", "");
            var val = new TimeSpan(int.Parse(day), int.Parse(hour), int.Parse(minute));
            return val;
        } 
    }
}
