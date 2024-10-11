using TaskTracker.Core.DataAccess.src.BaseClasses.impl;

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

        public int? Country { get; set; }
    }
}
