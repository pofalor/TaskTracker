namespace TaskTracker.Core.src.Enums
{
    /// <summary>
    /// Статус проверки рабочего пространства администратором
    /// </summary>
    public enum WorkspaceReviewStatus
    {
        /// <summary>
        /// Рабочее пространство ожидает проверки
        /// </summary>
        OnReview = 1,

        /// <summary>
        /// Рабочее пространство подтверждено администратором
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Рабочее пространство отклонено администратором
        /// </summary>
        Declined = 3,
    }
}
