using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog.Filters;
using NpgsqlTypes;
using System.Collections.Immutable;
using System.Linq;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class IssueService : IIssueService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<IssueService> _logger;
        private readonly IWorkspaceService _workSpaceService;
        private readonly ILogNotificatorService _logNotificatorService;

        public IssueService(ApplicationDbContext dbContext, ILogger<IssueService> logger, IWorkspaceService workSpaceService, 
            ILogNotificatorService logNotificatorService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _workSpaceService = workSpaceService;
            _logNotificatorService = logNotificatorService;
        }

        public async Task<IDataResult<List<IssueModel>>> GetProjectIssues(IssueFilter filter)
        {
            var result = new DataResult<List<IssueModel>>();

            try
            {
                //Вытаскиваем все задачи по проекту
                var issues = await _dbContext.Set<Issue>()
                    .AsNoTracking()
                    .Include(x => x.Project)
                    .Include(x=> x.Author)
                    .Include(x=> x.Assignee)
                    .Where(x => x.ProjectId == filter.ProjectId)
                    .Where(x => x.Project.WorkspaceId == filter.WorkspaceId)
                    .Where(x => !x.Project.IsDeleted)
                    .Where(x => !x.Project.Workspace.IsDeleted)
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                var issueIds = issues.Select(x => x.Id);

                var timeTrack = await _dbContext.Set<TimeTracking>()
                    .AsNoTracking()
                    .Where(x => issueIds.Contains(x.IssueId))
                    .Where(x => !x.IsDeleted)
                    .GroupBy(x => x.IssueId)
                    .ToDictionaryAsync(x => x.Key, y => new TimeSpan(y.SafeSum(z => z.TimeSpent.Ticks)));

                var models = issues.Select(x => new IssueModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Type = x.Type,
                    Status = x.Status,
                    Priority = x.Priority,
                    Estimate = x.Estimate.ToString(),
                    Index = x.Index,
                    EpicId = x.EpicId,
                    AuthorId = x.AuthorId,
                    AssigneeId = x.AssigneeId,
                    ProjectId = x.ProjectId,
                    TimeTrack = timeTrack.Get(x.Id, new TimeSpan()).ToString(),
                    ProjectCode = x.Project.Code,
                    AuthorName = x.Author.GetUserName(),
                    AssigneeName = x.Assignee?.GetUserName() ?? string.Empty
                })
                    .OrderByDescending(x=> x.Priority)
                    .ThenByDescending(x=> x.Id)
                    .ToList();

                return result.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting project issues.{NewLine}{Parameter}: {Filter}{NewLine2}",
                    Environment.NewLine, nameof(filter), filter?.ToJson(), Environment.NewLine);
                return result.WithError(IssueErrorCodes.CannotGetIssues);
            }
        }

        public async Task<IDataResult<bool>> CreateOrEdit(Issue request)
        {
            var result = new DataResult<bool>();
            try
            {
                //TODO: добавить проверку на EpicId
                if (request.ProjectId <= 0)
                {
                    return result.WithError(IssueErrorCodes.ProjectNotSet);
                }
                else if (request.AuthorId <= 0)
                {
                    return result.WithError(IssueErrorCodes.AuthorNotSet);
                }
                else if (request.Name.IsEmpty())
                {
                    return result.WithError(IssueErrorCodes.EmptyName);
                }
                else if (request.Description.IsEmpty())
                {
                    return result.WithError(IssueErrorCodes.EmptyDescr);
                }
                else if (!IssueConstants.ValidIssueTypes.Contains(request.Type))
                {
                    return result.WithError(IssueErrorCodes.IssueTypeInvalid);
                }
                else if (!IssueConstants.ValidIssuePriorities.Contains(request.Priority))
                {
                    return result.WithError(IssueErrorCodes.IssuePriorityInvalid);
                }
                else if (request.AssigneeId.HasValue && request.AssigneeId.Value <= 0)
                {
                    return result.WithError(IssueErrorCodes.IssueAssigneeInvalid);
                }

                var wspId = await _dbContext.Set<Project>()
                    .Where(x => request.ProjectId == x.Id)
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.Workspace.IsDeleted)
                    .Select(x => x.WorkspaceId)
                    .DefaultIfEmpty()
                    .FirstOrDefaultAsync();

                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(request.AuthorId, wspId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create issue, " +
                        $"but he not workspace membership{Environment.NewLine} " +
                        $"Project id: {request.ProjectId}{Environment.NewLine} " +
                        $"User id: {request.AuthorId}.");
                    return result.WithError(IssueErrorCodes.UserNotMemberWsp);
                }

                if (request.AssigneeId.HasValue)
                {
                    var isAssigneeWorkspaceMember = await _workSpaceService.IsWorkspaceMember(request.AssigneeId.Value, wspId);
                    if (!isAssigneeWorkspaceMember)
                    {
                        await _logNotificatorService.SendTelegramAdminAsync($"The user submitted a request to create an issue with an assignee " +
                            $"who is not a member of the workspace{Environment.NewLine}" +
                            $"Project id: {request.ProjectId}{Environment.NewLine} " +
                            $"User id: {request.AuthorId}{Environment.NewLine}" +
                            $"Assignee id: {request.AssigneeId}.");
                        return result.WithError(IssueErrorCodes.AssigneeNotMemberWsp);
                    }
                }

                var existingIssue = await _dbContext.Set<Issue>()
                    .Where(x => request.Id == x.Id)
                    .Where(x => !x.IsDeleted)
                    .FirstOrDefaultAsync();

                var newIssue = new Issue();

                //если не пусто, значит изменяем, иначе добавляем новую задачу
                if (existingIssue != null)
                {
                    newIssue = existingIssue;
                }
                else
                {
                    var lastIssueIndex = await _dbContext.Set<Issue>()
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted)
                        .Where(x => x.ProjectId == request.ProjectId)
                        .OrderByDescending(x => x.Index)
                        .Select(x => x.Index)
                        .FirstOrDefaultAsync();

                    newIssue.AuthorId = request.AuthorId;
                    newIssue.Index = lastIssueIndex + 1;
                }

                newIssue.Name = request.Name;
                newIssue.Description = request.Description;
                newIssue.Type = request.Type;
                newIssue.Status = request.Status;
                newIssue.Priority = request.Priority;
                newIssue.EpicId = request.EpicId;
                newIssue.AssigneeId = request.AssigneeId;
                newIssue.ProjectId = request.ProjectId;

                if (existingIssue == null)
                    await _dbContext.AddAsync(newIssue);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating issue.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(IssueErrorCodes.CannotCreateIssue);
            }
        }

        public async Task<IDataResult<bool>> TrackTime(TimeTracking request)
        {
            var result = new DataResult<bool>();

            try
            {
                if (request.TimeSpent == TimeSpan.Zero) 
                {
                    return result.WithError(IssueErrorCodes.TimeTrackIsZero);
                }
                else if(request.DateBegin == DateTime.MinValue)
                {
                    return result.WithError(IssueErrorCodes.TrackDateNotSet);
                }
                else if (request.DateBegin > DateTime.UtcNow)
                {
                    return result.WithError(IssueErrorCodes.TrackDateInFuture);
                }

                var existingIssue = await _dbContext.Set<Issue>()
                    .Include(x=> x.Project)
                    .Where(x => request.IssueId == x.Id)
                    .Where(x => !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if(existingIssue == null)
                {
                    return result.WithError(IssueErrorCodes.IssueNotSet);
                }

                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(request.UserId, existingIssue.Project.WorkspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to track time, " +
                        $"but he not workspace membership{Environment.NewLine}" +
                        $"Issue id: {request.IssueId}{Environment.NewLine}" +
                        $"Workspace id: {existingIssue.Project.WorkspaceId}{Environment.NewLine}" +
                        $"User id: {request.UserId}.");
                    return result.WithError(IssueErrorCodes.UserNotMemberWsp);
                }

                await _dbContext.AddAsync(request);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while tracking time.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(IssueErrorCodes.CannotCreateTimeTrack);
            }
        }

        public async Task<IDataResult<TimeTracking?>> GetActiveAutoTrack(int userId, int projectId)
        {
            var result = new DataResult<TimeTracking?>();

            try
            {
                var validStatuses = new AutoTrackTimeStatus[] { AutoTrackTimeStatus.Active, AutoTrackTimeStatus.Stopped };

                var activeTimeTrack = await _dbContext.Set<TimeTracking>()
                    .Where(x => x.AutoTrackStatus.HasValue)
                    .Where(x=> validStatuses.Contains(x.AutoTrackStatus.Value))
                    .Where(x=> x.UserId == userId)
                    .Where(x=> x.Issue.ProjectId == projectId)
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
                return result.WithError(IssueErrorCodes.CannotGetAutoTrack);
            }
        }
    }
}
