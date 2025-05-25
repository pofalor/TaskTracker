namespace TaskTracker.Core.src.Enums
{
    /// <summary>
    /// Статус автоматического трекинга времени
    /// </summary>
    public enum AutoTrackTimeStatus
    {
        /// <summary>
        /// Для фильтрации
        /// </summary>
        All = -1,

        /// <summary>
        /// Процесс трекинга запущен (идёт таймер)
        /// </summary>
        Active = 0,

        /// <summary>
        /// Процесс трекинга остановлен (таймер остановлен)
        /// </summary>
        Stopped = 1,

        /// <summary>
        /// Процесс трекинга успешно завершён
        /// </summary>
        Finished = 2
    }
}
