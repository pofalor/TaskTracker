using System.Globalization;
using TaskTracker.Core.src.Constants;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Models.Filters
{
    public class BaseFilter
    {
        public string Search
        {
            get
            {
                return search;
            }

            set
            {
                if (value.IsEmpty())
                {
                    search = string.Empty;
                }
                search = value.Trim().ToLower();
            }
        }
        private string search { get; set; } = string.Empty;
        public string BeginDateStr { get; set; } = string.Empty;
        public string EndDateStr { get; set; } = string.Empty;
        public DateTime? BeginDate
        {
            get
            {
                if (!BeginDateStr.IsEmpty()) return BeginDateStr.ParseDate();
                else return null;
            }
        }
        public DateTime? EndDate
        {
            get
            {
                if (!EndDateStr.IsEmpty()) return EndDateStr.ParseDate();
                else return null;
            }
        }
        public bool IsAdmin { get; set; }
        public int CryptoUserId { get; set; }

        public DateTime? AddDaysToBeginDate(int days)
        {
            //такое присвоение потому что значение этого поля по сути хранится в string 
            if (!BeginDate.HasValue) return null;

            var result = BeginDate.Value.AddDays(days);
            BeginDateStr = result.ToString(DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture);
            return BeginDate;
        }

        public DateTime? AddDaysToEndDate(int days)
        {
            //такое присвоение потому что значение этого поля по сути хранится в string 
            if (!EndDate.HasValue) return null;

            var result = EndDate.Value.AddDays(days);
            EndDateStr = result.ToString(DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture);
            return EndDate;
        }
    }
}
