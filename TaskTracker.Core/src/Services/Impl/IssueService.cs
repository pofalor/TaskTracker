using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
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
    public class IssueService : BaseService<Issue, IssueFilter>, IIssueService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<IssueService> _logger;
        private readonly IWorkSpaceService _workSpaceService;

        public IssueService(ApplicationDbContext dbContext, ILogger<IssueService> logger, IWorkSpaceService workSpaceService) :
            base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _workSpaceService = workSpaceService;
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
                    .Where(x => x.Project.WorkSpaceId == filter.WorkspaceId)
                    .Where(x => !x.Project.IsDeleted)
                    .Where(x => !x.Project.WorkSpace.IsDeleted)
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
                    .OrderByDescending(x=> x.Id)
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

        public override async Task<IDataResult<bool>> CreateOrEdit(Issue request)
        {
            var result = new DataResult<bool>();
            try
            {
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
                        .Where(x=> x.ProjectId ==  request.ProjectId)
                        .OrderByDescending(x=> x.Index)
                        .Select(x=> x.Index)
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
                var existingIssue = await _dbContext.Set<Issue>()
                      .Where(x => request.IssueId == x.Id)
                      .Where(x => !x.IsDeleted)
                      .FirstOrDefaultAsync();

                if(existingIssue == null)
                {
                    return result.WithError(IssueErrorCodes.IssueNotSet);
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
    }
}
