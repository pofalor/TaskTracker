using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskTracker.Core.src.BackgroundJobs
{
    public class RoleManagerBackgroundJob : BackgroundService
    {
        private readonly ILogger<RoleManagerBackgroundJob> _logger;

        public RoleManagerBackgroundJob(ILogger<RoleManagerBackgroundJob> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SetWorkspaceRoles(stoppingToken); // Предположим, что это асинхронная операция
                    await Task.Delay(60000, stoppingToken); //Ожидание 1 минута
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing the background task.");
                    // Обработка ошибки:  логирование, уведомления, задержка перед повторной попыткой и т.д.
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Задержка перед повторной попыткой
                }
            }
        }

        private async Task SetWorkspaceRoles(CancellationToken stoppingToken)
        {
            try
            {
                //1. Берём всех пользователей, кого добавили в воркспейс и кому не проставили роли (кого инвайтнули либо удалили). 
                //2. Берём всех, кому апрувнули реквест и проставляем роли 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the background task.");
                // Обработка ошибки:  логирование, уведомления, задержка перед повторной попыткой и т.д.
            }
        }
    }
}
