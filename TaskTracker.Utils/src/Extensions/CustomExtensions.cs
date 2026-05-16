using System.Text;

namespace TaskTracker.Utils.src.Extensions
{
    public static class CustomExtensions
    {
        /// <summary>
        /// Конвертировать строку в формате : 2h 3m 11s в TimeSpan
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Если строка  пустая, то вернётся пустой объект</returns>
        public static TimeSpan? ConvertToTSPNullCond(this string value)
        {
            if(string.IsNullOrEmpty(value))
                return null;

            var hour = value.GetNumberBeforeLetter("h");
            var minute = value.GetNumberBeforeLetter("m");
            var second = value.GetNumberBeforeLetter("s");
            var val = new TimeSpan(int.Parse(hour), int.Parse(minute), int.Parse(second));
            return val;
        }

        /// <summary>
        /// Конвертировать строку в формате : 2h 3m 11s в TimeSpan
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TimeSpan ConvertToTimespan(this string value)
        {
            var hour = value.GetNumberBeforeLetter("h");
            var minute = value.GetNumberBeforeLetter("m");
            var second = value.GetNumberBeforeLetter("s");
            var val = new TimeSpan(int.Parse(hour), int.Parse(minute), int.Parse(second));
            return val;
        }

        /// <summary>
        /// Форматировать TimeSpan в строку вида 2h 3m 11s
        /// </summary>
        public static string ToTimeTrackString(this TimeSpan value)
        {
            var totalSeconds = (int)value.TotalSeconds;
            var hours = totalSeconds / 3600;
            var minutes = totalSeconds % 3600 / 60;
            var seconds = totalSeconds % 60;
            return $"{hours}h {minutes}m {seconds}s";
        }

        public static string? ToTimeTrackStringNullCond(this TimeSpan? value)
        {
            return value.HasValue ? value.Value.ToTimeTrackString() : null;
        }

        private static string GetNumberBeforeLetter(this string str, string letterBeforeNumber)
        {
            var resultStr = new StringBuilder();
            if (!str.Contains(letterBeforeNumber))
            {
                return "0";
            }
            //-1 потому что обрабатываем предыдущий символ
            var startIndex = str.IndexOf(letterBeforeNumber) - 1;
            for (var i = startIndex; i >= 0; i--) 
            {
                var charVal = str[i].ToString();
                if(int.TryParse(charVal, out var parsed))
                {
                    resultStr.Insert(0, charVal);
                }
                else
                {
                    return resultStr.ToString();
                }
            }
            return resultStr.ToString();
        }
    }
}
