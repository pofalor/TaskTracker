using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTracker.Utils.src.Extensions
{
    public static class StringExtensions
    {
        // <summary>
        /// Проверка строки на пустоту
        /// </summary>
        /// <param name="str">Строка, проверяемая на пустоту</param>
        /// <returns></returns>
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
    }
}
