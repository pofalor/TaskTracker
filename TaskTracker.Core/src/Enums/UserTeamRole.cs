namespace TaskTracker.Core.src.Enums
{
    public enum UserTeamRole
    {
        //для фильтрации
        All = -1,

        /// <summary>
        /// Роль не задана
        /// </summary>
        NotSet = 0,

        Developer = 1,

        Tester = 2,

        Director = 3,

        /// <summary>
        /// Project manager
        /// </summary>
        ProjectMgr = 4, 

        Owner = 5
    }
}