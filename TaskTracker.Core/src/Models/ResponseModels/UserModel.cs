namespace TaskTracker.Core.src.Models.ResponseModels
{
    public class UserModel
    {
        public int Id { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Роль пользователя
        /// </summary>
        public IList<string> Roles { get; set; } = [];

        public int? Country { get; set; }

        /// <summary>
        /// Содержит никнейм, либо имя и фамилию, либо эмейл
        /// </summary>
        public string Name { get; set; } = null!;
    }
}
