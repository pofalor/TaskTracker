using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.ConfigSectionModels;
using TaskTracker.Core.src.DataResult;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TaskTracker.Core.src.Services.Impl
{
    public class LogNotificatorService : ILogNotificatorService
    {
        private readonly ILogger<LogNotificatorService> _logger;

        private readonly TelegramSettingsConfiguration TelegramConfig;

        public LogNotificatorService(ILogger<LogNotificatorService> logger, IConfiguration config)
        {
            _logger = logger;

            try
            {
                TelegramConfig = config.GetValue<TelegramSettingsConfiguration>(TelegramSettingsConfiguration.TelegramSectionInConfig)
                ?? throw new InvalidOperationException($"Cannot get {TelegramSettingsConfiguration.TelegramSectionInConfig} section from config. " +
                $"Value is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} fatal error! " +
                    "Msg: {Message}", nameof(LogNotificatorService), ex.Message);
                throw;
            }
        }

        public async Task<IDataResult<bool>> SendTelegramAdmin(string text)
        {
            var result = new DataResult<bool>();
            bool resp = true;
            foreach (var adminId in TelegramConfig.AdminTelegramIdsArray)
            {
                try
                {
                    resp &= (await SendBotTelegramAsync(text, adminId, TelegramConfig.TelegramBotToken)).Success;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{ServiceName} {MethodName}: " +
                        "Cannot send message to tg admin => [tgId = {AdminId}]", 
                        nameof(LogNotificatorService), nameof(SendTelegramAdmin), adminId);
                }
            }
            return result.WithData(resp);
        }

        public async Task<IDataResult<bool>> LogAndNotifyAdminsAsync(string text, Exception? exception = null)
        {
            var result = new DataResult<bool>();
            bool resp = true;
            try
            {
                _logger.LogError(exception, text);
                resp &= (await SendTelegramAdmin(text)).Success;
            }
            catch (Exception ex)
            {
                return result.WithError(ex.Message);
            }
            return result.WithData(resp);
        }

        public async Task<IDataResult<bool>> SendBotTelegramAsync(string text, 
            string userId, 
            string botAddress = "your_token_to_bot")
        {
            var result = new DataResult<bool>();
            try
            {
                var url = string.Format(
                    "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}",
                    botAddress, userId, text);


                using (var req = new HttpClient())
                {
                    var res = await req.GetAsync(url);
                    return result.WithData(res.IsSuccessStatusCode);
                }
            }
            catch (Exception ex)
            {
                string mes = $"{nameof(LogNotificatorService)} {nameof(SendBotTelegramAsync)}: " +
                    $"Cannot send message to tg => [tgId = {userId}]";
                _logger.LogError(ex, mes);
                return result.WithError(mes);
            }
        }
    }
}
