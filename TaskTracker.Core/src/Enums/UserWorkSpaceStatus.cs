namespace TaskTracker.Core.src.Enums
{
    public enum UserWorkspaceStatus
    {
        /// <summary>
        /// Для фильтрации
        /// </summary>
        All = -1,

        /// <summary>
        /// Юзер находится в этом рабочем пространстве
        /// </summary>
        Active = 1,

        /// <summary>
        /// Юзер удалён из рабочего пространства
        /// </summary>
        Deleted = 2,
    }
}
