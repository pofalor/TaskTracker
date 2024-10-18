using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTracker.Utils.src.Extensions
{
    public static class EnumerableExtensions
    {
        public static void Foreach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            ArgumentChecker.NotNull(enumerable, "enumerable");
            ArgumentChecker.NotNull(action, "action");

            foreach (var item in enumerable)
            {
                action(item);
            }
        }
    }
}
