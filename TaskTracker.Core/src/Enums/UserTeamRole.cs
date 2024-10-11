namespace TaskTracker.Core.src.Enums
{
    public enum UserTeamRole
    {
        //для фильтрации
        All = -1,

        Developer = 0,

        Tester = 1,

        Director = 2,

        /// <summary>
        /// Project manager
        /// </summary>
        ProjectMgr = 3
    }
}