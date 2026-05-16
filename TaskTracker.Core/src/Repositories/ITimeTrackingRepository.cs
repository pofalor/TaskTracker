namespace TaskTracker.Core.src.Repositories
{
    public interface ITimeTrackingRepository
    {
        /// <summary>
        /// Есть ли у задачи активный автоматический трек (таймер запущен или остановлен, но не завершён).
        /// </summary>
        Task<bool> HasActiveAutoTrackOnIssueAsync(int issueId, CancellationToken cancellationToken = default);

        Task<Dictionary<int, TimeSpan>> GetTimeSpentByIssueIdsAsync(
            IEnumerable<int> issueIds,
            CancellationToken cancellationToken = default);
    }
}
