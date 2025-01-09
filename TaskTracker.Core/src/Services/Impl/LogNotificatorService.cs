using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.ConfigSectionModels;
using TaskTracker.Core.src.DataResult;

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
                TelegramConfig = config.GetSection(TelegramSettingsConfiguration.TelegramSectionInConfig).Get<TelegramSettingsConfiguration>()
                ?? throw new InvalidOperationException($"Cannot get {TelegramSettingsConfiguration.TelegramSectionInConfig} section from config. " +
                $"Value is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} fatal error getting value from config!{NewLine}", 
                    nameof(LogNotificatorService), Environment.NewLine);
                throw;
            }
        }

        public async Task<IDataResult<bool>> SendTelegramAdminAsync(string text)
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
                    _logger.LogError(e, "Cannot send message to tg admin => [{Parameter} = {AdminId}]{NewLine}", 
                        nameof(adminId), adminId, Environment.NewLine);
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
                resp &= (await SendTelegramAdminAsync(text)).Success;
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
                string mes = $"Cannot send message to tg => [{nameof(userId)} = {userId}]";
                _logger.LogError(ex, mes);
                return result.WithError(mes);
            }
        }
    }
}
