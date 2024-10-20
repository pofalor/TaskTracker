namespace TaskTracker.Core.src.ConfigSectionModels
{
    public class IdentityConfiguration
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string IdentitySectionInConfig = "Identity";

        /// <summary>
        /// Кто выдал токен пользователю(по умолчанию - текущее приложение)
        /// </summary>
        public string TokenIssuer { get; set; } = string.Empty;

        /// <summary>
        /// Кому выдали токен(по умолчанию - фронт)
        /// </summary>
        public string TokenAudience { get; set; } = string.Empty;

        /// <summary>
        /// Ключ токена, по которому выполняется шифрование
        /// </summary>
        public string TokenSecret { get; set; } = string.Empty;
    }
}
