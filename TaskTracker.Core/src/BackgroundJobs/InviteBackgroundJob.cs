using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;

namespace TaskTracker.Core.src.BackgroundJobs
{
    public class InviteBackgroundJob : BackgroundService
    {
        private readonly ILogger<InviteBackgroundJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public InviteBackgroundJob(ILogger<InviteBackgroundJob> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CreateWorkspaceMembersAsync(stoppingToken); 
                await Task.Delay(2000, stoppingToken); //Ожидание 2 секунды
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

                using var scope = _scopeFactory.CreateScope();
                var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var activeRequests = await _dbContext.Set<WorkspaceInvite>()
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.IsChecked)
                    .Where(x => x.RequestStatus == InviteStatus.UserConfirmed)
                    .ToArrayAsync(stoppingToken);

                if (activeRequests.Length == 0)
                {
                    return;
                }

                var userIds = activeRequests.Select(x => x.UserId).ToArray();
                var wspIds = activeRequests.Select(x => x.WorkspaceId).ToArray();
                var userAndWspIds = activeRequests.Select(x => $"{x.UserId}_{x.WorkspaceId}").ToArray();

                var query = await _dbContext.Set<WorkspaceMember>()
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.Workspace.IsDeleted)
                    .Where(x => userIds.Contains(x.UserId))
                    .Where(x => wspIds.Contains(x.WorkspaceId))
                    .ToArrayAsync(stoppingToken);

                var workspaceMembers = query
                    .Where(x => userAndWspIds.Contains($"{x.UserId}_{x.WorkspaceId}"))
                    .GroupBy(x => new { x.UserId, x.WorkspaceId })
                    .ToDictionary(x => x.Key, y => y.FirstOrDefault());

                foreach (var request in activeRequests)
                {
                    try
                    {
                        if (workspaceMembers.TryGetValue(new { request.UserId, request.WorkspaceId }, out var workspaceMember))
                        {
                            workspaceMember.UserStatus = UserWorkspaceStatus.Active;
                        }
                        else
                        {
                            workspaceMember = new WorkspaceMember()
                            {
                                TeamRole = UserTeamRole.NotSet,
                                UserStatus = UserWorkspaceStatus.Active,
                                UserId = request.UserId,
                                WorkspaceId = request.WorkspaceId
                            };

                            request.IsChecked = true;
                            await _dbContext.AddAsync(workspaceMember);
                            await _dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while creating WorkspaceMember. Invite id: {IviteId}.", request.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                using var scope = _scopeFactory.CreateScope();
                var _logNotificatorService = scope.ServiceProvider.GetRequiredService<ILogNotificatorService>();
                await _logNotificatorService.LogAndNotifyAdminsAsync($"An error occurred while executing the background task: {nameof(CreateWorkspaceMembersAsync)}", ex);
            }
        }
    }
}
