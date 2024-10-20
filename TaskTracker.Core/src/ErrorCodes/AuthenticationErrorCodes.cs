using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Resources.ErrorCodes;

namespace TaskTracker.Core.src.ErrorCodes
{
    public enum AuthenticationErrorCodes
    {
        /// <summary>
        /// Юзер уже существует
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(UserAlreadyExists))]
        UserAlreadyExists = BaseErrorCodes.GetItemsError + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Не удалось создать пользователя.
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(ErrorCreatingUser))]
        ErrorCreatingUser = UserAlreadyExists + 1,

        /// <summary>
        /// Неверное имя пользователя.
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(ErrorFirstName))]
        ErrorFirstName = ErrorCreatingUser + 1,

        /// <summary>
        /// Неверная фамилия пользователя.
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(ErrorLastName))]
        ErrorLastName = ErrorFirstName + 1,

        /// <summary>
        /// Неверный эмейл.
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(InvalidEmail))]
        InvalidEmail = ErrorLastName + 1,

        /// <summary>
        /// Неверный эмейл или пароль(нужно для аутентификации)
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(InvalidEmailOrPassword))]
        InvalidEmailOrPassword = InvalidEmail + 1,

        /// <summary>
        /// Не удалось выполнить вход.
        /// </summary>
        [ErrorMessage(typeof(AuthenticationErrorCodeResources), nameof(AuthError))]
        AuthError = InvalidEmailOrPassword + 1,
    }
}
