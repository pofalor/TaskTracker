using Microsoft.Extensions.Configuration;
using TaskTracker.Core.src.DataResult;

namespace TaskTracker.Core.src.Services
{
    public interface ILogNotificatorService
    {
        Task<IDataResult<bool>> SendTelegramAdminAsync(string text);

        Task<IDataResult<bool>> LogAndNotifyAdminsAsync(string text, Exception? exception = null);
    }
}
