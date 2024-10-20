using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTracker.Core.src.ConfigSectionModels
{
    public class TelegramSettingsConfiguration
    {
        /// <summary>
        /// Название секции конфигурации по умолчанию
        /// </summary>
        public const string TelegramSectionInConfig = "TelegramSettings";

        /// <summary>
        /// Телеграм айди админов в телеге
        /// </summary>
        public string AdminTelegramIds { get; set; } = string.Empty;

        /// <summary>
        /// Токен телеграм бота
        /// </summary>
        public string TelegramBotToken { get; set; } = string.Empty;

        public string[] AdminTelegramIdsArray
        {
            get
            {
                return AdminTelegramIds.Split(',');
            }
        }
    }
}
