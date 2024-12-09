namespace TaskTracker.Core.src.Enums
{
    public enum UserStatusChangeType
    {
        All = -1,

        /// <summary>
        /// С этим статусом создаются запросы 
        /// </summary>
        Default = 0,

        UserConfirmed = 1,

        UserDeclined = 2,
    }
}
