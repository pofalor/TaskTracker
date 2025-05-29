using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.Services
{
    public interface IAutoTimeTrackService
    {
        /// <summary>
        /// Получить активный автоматический трек, если он есть. Если нет, то возвращается null.
        /// </summary>
        Task<IDataResult<TimeTracking?>> GetActiveAutoTrack(int userId, int projectId);

        /// <summary>
        /// Начать автоматический трекинг времени
        /// </summary>
        Task<IDataResult<TimeTracking>> StartTracking(TimeTracking request);

        /// <summary>
        /// Остановить автоматический трекинг времени
        /// </summary>
        Task<IDataResult<TimeTracking>> StopTracking(TimeTracking request);
    }
}
