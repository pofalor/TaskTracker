using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Repositories;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class IssueService : IIssueService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<IssueService> _logger;
        private readonly IWorkspaceService _workSpaceService;
        private readonly ILogNotificatorService _logNotificatorService;
        private readonly ITimeTrackingRepository _timeTrackingRepository;
        private readonly IIssueRepository _issueRepository;

        public IssueService(
            ApplicationDbContext dbContext,
            ILogger<IssueService> logger,
            IWorkspaceService workSpaceService,
            ILogNotificatorService logNotificatorService,
            ITimeTrackingRepository timeTrackingRepository,
            IIssueRepository issueRepository)
        {
            _dbContext = dbContext;
            _logger = logger;
            _workSpaceService = workSpaceService;
            _logNotificatorService = logNotificatorService;
            _timeTrackingRepository = timeTrackingRepository;
            _issueRepository = issueRepository;
        }

        public async Task<IDataResult<List<IssueModel>>> GetProjectIssues(IssueFilter filter)
        {
            var result = new DataResult<List<IssueModel>>();

            try
            {
                var issues = await _issueRepository.GetProjectIssuesAsync(filter.ProjectId, filter.WorkspaceId);

                var issueIds = issues.Select(x => x.Id);

                var timeTrack = await _timeTrackingRepository.GetTimeSpentByIssueIdsAsync(issueIds);

                var issueKeys = issues.ToDictionary(
                    x => x.Id,
                    x => $"{x.Project.Code}-{x.Index}");

                var childKeysByParent = issues
                    .Where(x => x.ParentId.HasValue)
                    .GroupBy(x => x.ParentId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => issueKeys[x.Id]).OrderBy(k => k).ToList());

                var models = issues.Select(x => new IssueModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Type = x.Type,
                    Status = x.Status,
                    Priority = x.Priority,
                    Estimate = x.Estimate.ToTimeTrackStringNullCond(),
                    Index = x.Index,
                    ParentId = x.ParentId,
                    ParentKey = x.ParentId.HasValue && issueKeys.TryGetValue(x.ParentId.Value, out var parentKey)
                        ? parentKey
                        : null,
                    ChildIssueKeys = childKeysByParent.Get(x.Id, new List<string>()),
                    AuthorId = x.AuthorId,
                    AssigneeId = x.AssigneeId,
                    ProjectId = x.ProjectId,
                    TimeTrack = timeTrack.Get(x.Id, TimeSpan.Zero).ToTimeTrackString(),
                    ProjectCode = x.Project.Code,
                    AuthorName = x.Author.GetUserName(),
                    AssigneeName = x.Assignee?.GetUserName() ?? string.Empty
                })
                    .OrderByDescending(x => x.Priority)
                    .ThenByDescending(x => x.Id)
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

        public async Task<IDataResult<bool>> CreateIssue(Issue request)
        {
            var result = new DataResult<bool>();
            try
            {
                var validationError = ValidateIssueRequest(request, isUpdate: false);
                if (validationError.HasValue)
                {
                    return result.WithError(validationError.Value);
                }

                var wspId = await GetWorkspaceIdByProjectIdAsync(request.ProjectId);
                if (wspId <= 0)
                {
                    return result.WithError(IssueErrorCodes.ProjectNotSet);
                }

                var assigneeError = await ValidateAssigneeMembershipAsync(request, wspId);
                if (assigneeError.HasValue)
                {
                    return result.WithError(assigneeError.Value);
                }

                var parentError = await ValidateParentIssueAsync(request.ParentId, request.ProjectId, request.Id);
                if (parentError.HasValue)
                {
                    return result.WithError(parentError.Value);
                }

                var lastIssueIndex = await _issueRepository.GetNextIndexAsync(request.ProjectId);

                var issue = new Issue
                {
                    AuthorId = request.AuthorId,
                    Index = lastIssueIndex + 1,
                    Name = request.Name,
                    Description = request.Description,
                    Type = request.Type,
                    Status = request.Status,
                    Priority = request.Priority,
                    ParentId = request.ParentId,
                    AssigneeId = request.AssigneeId,
                    ProjectId = request.ProjectId,
                    Estimate = request.Estimate,
                };

                await _issueRepository.AddAsync(issue);
                await _issueRepository.AddStatusHistoryAsync(new IssueStatusHistory
                {
                    Issue = issue,
                    OldStatus = null,
                    NewStatus = issue.Status,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = request.AuthorId,
                });
                await _issueRepository.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating issue.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(IssueErrorCodes.CannotCreateIssue);
            }
        }

        public async Task<IDataResult<bool>> UpdateIssue(Issue request, int userId)
        {
            var result = new DataResult<bool>();
            try
            {
                var validationError = ValidateIssueRequest(request, isUpdate: true);
                if (validationError.HasValue)
                {
                    return result.WithError(validationError.Value);
                }

                var existingIssue = await _issueRepository.GetByIdNotDeletedAsync(request.Id);

                if (existingIssue == null)
                {
                    return result.WithError(IssueErrorCodes.IssueNotSet);
                }

                var wspId = await GetWorkspaceIdByProjectIdAsync(existingIssue.ProjectId);
                if (wspId <= 0)
                {
                    return result.WithError(IssueErrorCodes.ProjectNotSet);
                }

                var assigneeError = await ValidateAssigneeMembershipAsync(request, wspId);
                if (assigneeError.HasValue)
                {
                    return result.WithError(assigneeError.Value);
                }

                var parentError = await ValidateParentIssueAsync(request.ParentId, request.ProjectId, request.Id);
                if (parentError.HasValue)
                {
                    return result.WithError(parentError.Value);
                }

                var statusChangeError = await ValidateStatusChangeAsync(existingIssue, request);
                if (statusChangeError.HasValue)
                {
                    return result.WithError(statusChangeError.Value);
                }

                var oldStatus = existingIssue.Status;
                var statusChanged = oldStatus != request.Status;

                existingIssue.Name = request.Name;
                existingIssue.Description = request.Description;
                existingIssue.Type = request.Type;
                existingIssue.Status = request.Status;
                existingIssue.Priority = request.Priority;
                existingIssue.ParentId = request.ParentId;
                existingIssue.AssigneeId = request.AssigneeId;
                existingIssue.ProjectId = request.ProjectId;
                existingIssue.Estimate = request.Estimate;

                if (statusChanged)
                {
                    await _issueRepository.AddStatusHistoryAsync(new IssueStatusHistory
                    {
                        IssueId = existingIssue.Id,
                        OldStatus = oldStatus,
                        NewStatus = request.Status,
                        ChangedAt = DateTime.UtcNow,
                        ChangedByUserId = userId,
                    });
                }

                await _issueRepository.UpdateAsync(existingIssue);
                await _issueRepository.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating issue.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(IssueErrorCodes.CannotUpdateIssue);
            }
        }

        private static IssueErrorCodes? ValidateIssueRequest(Issue request, bool isUpdate)
        {
            if (isUpdate && request.Id <= 0)
            {
                return IssueErrorCodes.IssueNotSet;
            }

            if (request.ProjectId <= 0)
            {
                return IssueErrorCodes.ProjectNotSet;
            }

            if (!isUpdate && request.AuthorId <= 0)
            {
                return IssueErrorCodes.AuthorNotSet;
            }

            if (request.Name.IsEmpty())
            {
                return IssueErrorCodes.EmptyName;
            }

            if (!IssueConstants.ValidIssueTypes.Contains(request.Type))
            {
                return IssueErrorCodes.IssueTypeInvalid;
            }

            if (!IssueConstants.ValidIssuePriorities.Contains(request.Priority))
            {
                return IssueErrorCodes.IssuePriorityInvalid;
            }

            if (request.AssigneeId.HasValue && request.AssigneeId.Value <= 0)
            {
                return IssueErrorCodes.IssueAssigneeInvalid;
            }

            if (request.Estimate.HasValue && request.Estimate <= TimeSpan.Zero)
            {
                return IssueErrorCodes.EstimateZeroOrLess;
            }

            return null;
        }

        private async Task<IssueErrorCodes?> ValidateParentIssueAsync(int? parentId, int projectId, int issueId)
        {
            if (!parentId.HasValue)
            {
                return null;
            }

            if (parentId.Value <= 0)
            {
                return IssueErrorCodes.ParentIssueInvalid;
            }

            if (issueId > 0 && parentId.Value == issueId)
            {
                return IssueErrorCodes.ParentCannotBeSelf;
            }

            var parentExistsInProject = await _issueRepository.ExistsInProjectAsync(parentId.Value, projectId);
            if (!parentExistsInProject)
            {
                var parentIssue = await _issueRepository.GetByIdNotDeletedAsync(parentId.Value);
                return parentIssue == null
                    ? IssueErrorCodes.ParentIssueNotSet
                    : IssueErrorCodes.ParentIssueWrongProject;
            }

            return null;
        }

        private async Task<int> GetWorkspaceIdByProjectIdAsync(int projectId)
        {
            return await _dbContext.Set<Project>()
                .AsNoTracking()
                .Where(x => projectId == x.Id)
                .Where(x => !x.IsDeleted)
                .Where(x => !x.Workspace.IsDeleted)
                .Select(x => x.WorkspaceId)
                .FirstOrDefaultAsync();
        }

        private async Task<IssueErrorCodes?> ValidateAssigneeMembershipAsync(Issue request, int workspaceId)
        {
            if (!request.AssigneeId.HasValue)
            {
                return null;
            }

            var isAssigneeWorkspaceMember = await _workSpaceService.IsWorkspaceMember(request.AssigneeId.Value, workspaceId);
            if (isAssigneeWorkspaceMember)
            {
                return null;
            }

            await _logNotificatorService.SendTelegramAdminAsync(
                $"The user submitted a request to save an issue with an assignee who is not a member of the workspace{Environment.NewLine}" +
                $"Project id: {request.ProjectId}{Environment.NewLine}" +
                $"User id: {request.AuthorId}{Environment.NewLine}" +
                $"Assignee id: {request.AssigneeId}.");

            return IssueErrorCodes.AssigneeNotMemberWsp;
        }

        private async Task<IssueErrorCodes?> ValidateStatusChangeAsync(Issue existingIssue, Issue request)
        {
            if (existingIssue.Status == request.Status)
            {
                return null;
            }

            var hasActiveAutoTrack = await _timeTrackingRepository.HasActiveAutoTrackOnIssueAsync(existingIssue.Id);
            if (hasActiveAutoTrack)
            {
                return IssueErrorCodes.IssueStatusLockedByAutoTrack;
            }

            return null;
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
                    .AsNoTracking()
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

                var existingTimeTrack = await _dbContext.Set<TimeTracking>()
                    .Where(x => request.Id == x.Id)
                    .Where(x => !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingTimeTrack != null)
                {
                    existingTimeTrack.Comment = request.Comment;
                    existingTimeTrack.TimeSpent = request.TimeSpent;

                    if (existingTimeTrack.AutoTrackStatus.HasValue)
                    {
                        existingTimeTrack.AutoTrackStatus = AutoTrackTimeStatus.Finished;
                    }
                }
                else
                {
                    await _dbContext.AddAsync(request);
                }
                
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
    }
}
