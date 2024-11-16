using TaskTracker.Core.src.DataAccess.BaseClasses;

namespace TaskTracker.Core.src.Entities
{
    public class User : PersistentEntity
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public int? Country { get; set; }

        public string NickName { get; set; } = string.Empty;

        public ICollection<WorkSpace> WorkSpaces { get; } = [];
    }
}
