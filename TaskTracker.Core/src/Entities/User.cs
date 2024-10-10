using TaskTracker.Core.DataAccess.src.BaseClasses.impl;

namespace TaskTracker.Core.src.Entities
{
    public class User : PersistentEntity<int>
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
    }
}
