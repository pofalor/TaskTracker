using Microsoft.AspNetCore.Identity;
using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Utils.src.Extensions;

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

        /// <summary>
        /// возврат - фамилия и имя текущего пользователя, либо Nickname
        /// </summary>
        /// <returns></returns>
        public string GetUserName(bool isAdmin = false)
        {
            var resultName = "";

            if (MustUseNickname() && !isAdmin) resultName = NickName;
            else if (!FirstName.Trim().IsEmpty() && !LastName.Trim().IsEmpty())
            {
                resultName = $"{LastName} {FirstName}";
            }
            else resultName = Email;

            return resultName;
        }

        /// <summary>
        /// true - если Nickname заполнен
        /// </summary>
        public bool MustUseNickname()
        {
            return !NickName.IsEmpty();
        }
    }
}
