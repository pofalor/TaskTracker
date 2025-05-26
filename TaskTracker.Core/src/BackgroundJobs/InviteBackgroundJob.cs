using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.BackgroundJobs
{
    public class InviteBackgroundJob : BackgroundService
    {
        private readonly ILogger<InviteBackgroundJob> _logger;
        private readonly ApplicationDbContext _dbContext;
        public InviteBackgroundJob(ApplicationDbContext dbContext, ILogger<InviteBackgroundJob> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CreateWorkspaceMembersAsync(stoppingToken); // Предположим, что это асинхронная операция
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

        private async Task CreateWorkspaceMembersAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    // Если отмена запрошена во время выполнения логики,
                    // немедленно прекращаем выполнение и возвращаемся
                    _logger.LogInformation($"{nameof(CreateWorkspaceMembersAsync)} cancelled.");
                    return; 
                }

                var activeRequests = await _dbContext.Set<WorkspaceInvite>()
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.IsChecked)
                    .Where(x => x.RequestStatus == InviteStatus.UserConfirmed)
                    .ToArrayAsync(stoppingToken);

                if (activeRequests.Length == 0)
                {
                    return;
                }

                var userIds = activeRequests.Select(x=> x.UserId).ToArray();
                var wspIds = activeRequests.Select(x=> x.WorkSpaceId).ToArray();
                var userAndWspIds = activeRequests.Select(x => $"{x.UserId}_{x.WorkSpaceId}");

                var workspaceMembers = await _dbContext.Set<WorkSpaceMember>()
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted)
                        .Where(x => !x.WorkSpace.IsDeleted)
                        .Where(x => userIds.Contains(x.UserId))
                        .Where(x => wspIds.Contains(x.WorkSpaceId))
                        .GroupBy(x => x.UserId + "_" + x.WorkSpaceId)
                        .ToDictionaryAsync(x=> x.Key, y=> y.FirstOrDefault(), stoppingToken);

                foreach(var request in activeRequests)
                {
                    try
                    {
                        if (workspaceMembers.TryGetValue($"{request.UserId}_{request.WorkSpaceId}", out var workspaceMember))
                        {
                            workspaceMember.UserStatus = UserWorkSpaceStatus.Active;
                        }
                        else
                        {
                            workspaceMember = new WorkSpaceMember()
                            {
                                TeamRole = UserTeamRole.NotSet,
                                UserStatus = UserWorkSpaceStatus.Active,
                                UserId = request.UserId,
                                WorkSpaceId = request.WorkSpaceId
                            };

                            await _dbContext.AddAsync(workspaceMember);
                        }
                    }
                    catch { }
                }   
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the background task.");
                // Обработка ошибки:  логирование, уведомления, задержка перед повторной попыткой и т.д.
            }
        }
    }
}
