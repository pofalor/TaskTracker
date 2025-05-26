using System.Reflection;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.ErrorCodes
{
    public static class ErrorMessageManager
    {
        private static readonly Dictionary<int, string> ErrorMessageStorage = [];

        static ErrorMessageManager()
        {
            RegisterEnum<SystemErrorCodes>();
            RegisterEnum<BaseErrorCodes>();
            RegisterEnum<AuthenticationErrorCodes>();
            RegisterEnum<SosErrorCodes>();
            RegisterEnum<UserErrorCodes>();
            RegisterEnum<WorkspaceErrorCodes>();
            RegisterEnum<ProjectErrorCodes>();
            RegisterEnum<IssueErrorCodes>();
        }

        /// <summary>
        /// Получить сообщение об ошибке по коду
        /// </summary>
        /// <param name="errorCode">Код ошибки</param>
        /// <returns>Сообщение об ошибке</returns>
        public static string GetErrorMessage(int errorCode)
        {
            return ErrorMessageStorage.Get(errorCode, string.Empty);
        }

        private static void RegisterEnum<T>() where T : struct, IConvertible
        {
            var type = typeof(T);

            if (!type.IsEnum)
            {
                throw new InvalidOperationException();
            }

            foreach (var @enum in type.GetFields())
            {
                if (!@enum.IsPublic)
                {
                    continue;
                }

                var code = Convert.ToInt32(@enum.GetValue(new T()));

                var attr = (ErrorMessageAttribute?)@enum.GetCustomAttribute(typeof(ErrorMessageAttribute));

                if (attr != null)
                {
                    ErrorMessageStorage[code] = attr.GetErrorMessage();
                }
            }
        }
    }
}