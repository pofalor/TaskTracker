namespace TaskTracker.Utils.src.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Преобразовать переданный объект в Int32
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public static int ToInt(this object obj, int defaultValue = default(int))
        {
            if (obj == null)
            {
                return defaultValue;
            }

            if (obj is int v)
            {
                return v;
            }

            int val;

            if (int.TryParse(obj.ToString(), out val))
            {
                return val;
            }

            return defaultValue;
        }
    }
}
