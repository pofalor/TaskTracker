using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class AutoTimeTrackService : IAutoTimeTrackService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AutoTimeTrackService> _logger;
        private readonly IWorkspaceService _workSpaceService;
        private readonly ILogNotificatorService _logNotificatorService;

        public AutoTimeTrackService(ApplicationDbContext dbContext, ILogger<AutoTimeTrackService> logger, IWorkspaceService workSpaceService,
            ILogNotificatorService logNotificatorService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _workSpaceService = workSpaceService;
            _logNotificatorService = logNotificatorService;
        }

        public async Task<IDataResult<TimeTracking?>> GetActiveAutoTrack(int userId, int projectId)
        {
            var result = new DataResult<TimeTracking?>();

            try
            {
                var validStatuses = new AutoTrackTimeStatus[] { AutoTrackTimeStatus.Active, AutoTrackTimeStatus.Stopped };

                var activeTimeTrack = await _dbContext.Set<TimeTracking>()
                    .AsNoTracking()
                    .Where(x => x.AutoTrackStatus.HasValue)
                    .Where(x => validStatuses.Contains(x.AutoTrackStatus.Value))
                    .Where(x => x.UserId == userId)
                    .Where(x => x.Issue.ProjectId == projectId)
                    .Where(x => !x.IsDeleted)
                    .FirstOrDefaultAsync();

                return result.WithData(activeTimeTrack);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting active auto track.{NewLine}" +
                    "{Parameter}: {UserId}{NewLine2}" +
                    "{Parameter2}: {ProjectId}",
                    Environment.NewLine, nameof(userId), userId, Environment.NewLine, nameof(projectId), projectId);
                return result.WithError(AutoTimeTrackErrorCodes.CannotGetAutoTrack);
            }
        }

        public async Task<IDataResult<TimeTracking>> StartTracking(TimeTracking request)
        {
            var result = new DataResult<TimeTracking>();

            try
            {
                if(request.TimeSpent > TimeSpan.Zero)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.TimeSpentInvalid);
                }
                if (request.DateBegin > DateTime.UtcNow)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.DateBeginInFuture);
                }

                var existingIssue = await _dbContext.Set<Issue>()
                    .AsNoTracking()
                    .Include(x => x.Project)
                    .Where(x => request.IssueId == x.Id)
                    .Where(x => !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingIssue == null)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.IssueNotSet);
                }

                if (existingIssue.AssigneeId != request.UserId)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.InalidAssignee);
                }
                if (existingIssue.Status != IssueStatus.InProgress)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.InvalidIssueStatus);
                }

                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(request.UserId, existingIssue.Project.WorkspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to start auto tracking, " +
                        $"but he not workspace membership{Environment.NewLine}" +
                        $"Issue id: {request.IssueId}{Environment.NewLine}" +
                        $"Workspace id: {existingIssue.Project.WorkspaceId}{Environment.NewLine}" +
                        $"User id: {request.UserId}.");
                    return result.WithError(AutoTimeTrackErrorCodes.UserNotMemberWsp);
                }

                var existingTimeTrack = await GetActiveAutoTrack(request.UserId, existingIssue.ProjectId);

                if (!existingTimeTrack.Success)
                {
                    return result.WithError(existingTimeTrack.Errors[0].Message);
                }

                if (existingTimeTrack.Data != null)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.AutoTrackExists);
                }

                request.AutoTrackStatus = AutoTrackTimeStatus.Active;

                await _dbContext.AddAsync(request);
                await _dbContext.SaveChangesAsync();

                return result.WithData(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting auto tracking.{NewLine}" +
                    "{Parameter}: {Request}",
                    Environment.NewLine, nameof(request), request?.ToJson());
                return result.WithError(AutoTimeTrackErrorCodes.CannotStartAutoTrack);
            }
        }

        public async Task<IDataResult<TimeTracking>> StopTracking(TimeTracking request)
        {
            var result = new DataResult<TimeTracking>();

            try
            {
                if (request.TimeSpent == TimeSpan.Zero)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.TimeSpentInvalid);
                }

                var existingTimeTrack = await _dbContext.Set<TimeTracking>()
                    .Include(x=> x.Issue.Project)
                    .Where(x => request.Id == x.Id)
                    .Where(x => !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingTimeTrack == null)
                {
                    return result.WithError(AutoTimeTrackErrorCodes.CannotFindActiveTrack);
                }

                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(request.UserId, existingTimeTrack.Issue.Project.WorkspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to stop auto tracking, " +
                        $"but he not workspace membership{Environment.NewLine}" +
                        $"Issue id: {request.IssueId}{Environment.NewLine}" +
                        $"Workspace id: {existingTimeTrack.Issue.Project.WorkspaceId}{Environment.NewLine}" +
                        $"User id: {request.UserId}.");
                    return result.WithError(AutoTimeTrackErrorCodes.UserNotMemberWsp);
                }

                existingTimeTrack.AutoTrackStatus = AutoTrackTimeStatus.Stopped;
                existingTimeTrack.TimeSpent = request.TimeSpent;

                await _dbContext.SaveChangesAsync();

                return result.WithData(existingTimeTrack);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping auto tracking.{NewLine}" +
                    "{Parameter}: {Request}",
                    Environment.NewLine, nameof(request), request?.ToJson());
                return result.WithError(AutoTimeTrackErrorCodes.CannotStopAutoTrack);
            }
        }
    }
}
