using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Resources.ErrorCodes;

namespace TaskTracker.Core.src.Enums.ErrorCodes
{
    public enum UserErrorCodes
    {
        /// <summary>
        /// Не удалось получить юзера
        /// </summary>
        [ErrorMessage(typeof(UserErrorCodeResources), nameof(CannotGetUser))]
        CannotGetUser = SosErrorCodes.RoleNameNullError + ErrorConstants.EnumErrorCodeCount,
    }
}
