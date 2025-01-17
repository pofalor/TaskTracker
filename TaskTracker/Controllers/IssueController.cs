using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Controllers.BaseControllers;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/issue")]
    public class IssueController : BaseController<Issue, IssueModel, CreateOrEditIssuePR, IssueFilter>
    {
        private readonly ILogger<IssueController> _logger;
        private readonly IWorkSpaceService _workSpaceService;
        private readonly IIssueService _issueService;
        private readonly IMapper _mapper;
        private readonly ILogNotificatorService _logNotificatorService;

        public IssueController(ILogger<IssueController> logger, IWorkSpaceService workSpaceService,
            IMapper mapper, IUserService userService, ILogNotificatorService logNotificatorService, IIssueService issueService)
            : base(logger, issueService, mapper, userService)
        {
            _logger = logger;
            _workSpaceService = workSpaceService;
            _mapper = mapper;
            _logNotificatorService = logNotificatorService;
            _issueService = issueService;
        }

        [Route("getProjectIssues")]
        [HttpPost]
        public async Task<DataResponse<List<IssueModel>>> GetProjectIssues(IssueFilter filter)
        {
            var response = new DataResponse<List<IssueModel>>();

            try
            {
                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(UserId, filter.WorkspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to get a project issues, " +
                        $"but he not workspace membership{Environment.NewLine} " +
                        $"Project id: {filter.ProjectId}{Environment.NewLine} " +
                        $"User id: {UserId}.");
                    return response.WithError(IssueErrorCodes.UserNotMemberWsp);
                }

                var result = await _issueService.GetProjectIssues(filter);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting issues => {Parameter1}: {Filter},", nameof(filter), filter?.ToJson());
                return response.WithError(IssueErrorCodes.CannotGetIssues);
            }
        }

        public override void InitRoles()
        {
            AddRole(nameof(CreateOrEdit), Permissions.UserRole);
        }

        [Route("add")]
        [HttpPost]
        public override async Task<DataResponse<bool>> CreateOrEdit(CreateOrEditIssuePR request)
        {
            var response = new DataResponse<bool>();

            var isSuccess = await CheckRoles(nameof(CreateOrEdit));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {

                if (!request.AuthorId.HasValue)
                    request.AuthorId = UserId;

                if (request.AuthorId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create a issue with a user Id " +
                        $"different from his user Id in claims{Environment.NewLine} " +
                        $"User id from request: {request.AuthorId}{Environment.NewLine} " +
                        $"User id from claims: {UserId}.");
                    return response.WithError(ProjectErrorCodes.AccessDenied);
                }

                var mapRes = _mapper.Map<Issue>(request);
                var result = await _issueService.CreateOrEdit(mapRes);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to add or change issue.{NewLine}{Parameter}:{Request}{NewLine2}",
                   Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return response.WithError(IssueErrorCodes.CannotCreateIssue);
            }
        }

        [Route("trackTime")]
        [HttpPost]
        public async Task<DataResponse<bool>> TrackTime(TimeTrackPR request)
        {
            var response = new DataResponse<bool>();

            try
            {
                if (!request.UserId.HasValue)
                    request.UserId = UserId;

                var mapRes = _mapper.Map<TimeTracking>(request);
                var result = await _issueService.TrackTime(mapRes);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to track time.{NewLine}{Parameter}:{Request}{NewLine2}",
                   Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return response.WithError(IssueErrorCodes.CannotCreateTimeTrack);
            }
        }
    }
}
