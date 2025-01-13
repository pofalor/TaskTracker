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
    [Route("api/workspace")]
    public class WorkSpaceController : BaseController<WorkSpace, WorkSpaceModel, CreateOrEditWorkSpacePostRequest, WorkSpaceFilter>
    {
        private readonly ILogger<WorkSpaceController> _logger;
        private readonly IWorkSpaceService _workSpaceService;
        private readonly IMapper _mapper;
        private readonly ILogNotificatorService _logNotificatorService;

        public WorkSpaceController(ILogger<WorkSpaceController> logger, IWorkSpaceService workSpaceService,
            IMapper mapper, IUserService userService, ILogNotificatorService logNotificatorService) : base(logger, workSpaceService, mapper, userService)
        {
            _logger = logger;
            _workSpaceService = workSpaceService;
            _mapper = mapper;
            _logNotificatorService = logNotificatorService;
        }

        public override void InitRoles()
        {
            AddRole(nameof(CreateOrEdit), Permissions.UserRole);
            AddRole(nameof(CreateWspInvite), Permissions.UserRole);
        }

        [Route("getMyWorkspaces")]
        [HttpGet]
        public async Task<DataResponse<List<WorkSpaceModel>>> GetMyWorkspaces()
        {
            var response = new DataResponse<List<WorkSpaceModel>>();

            try
            {
                var result = await _workSpaceService.GetMyWorkspaces(UserId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<WorkSpaceModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting my workspaces => {Parameter1}: {UserId},", nameof(UserId), UserId);
                return response.WithError(WorkSpaceErrorCodes.CannotGetMyWorkspaces);
            }
        }


        [Route("add")]
        [HttpPost]
        public override async Task<DataResponse<bool>> CreateOrEdit(CreateOrEditWorkSpacePostRequest request)
        {
            var response = new DataResponse<bool>();

            var isSuccess = await CheckRoles(nameof(CreateOrEdit));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {
                if (!request.DirectorUserId.HasValue)
                    request.DirectorUserId = UserId;

                if(request.DirectorUserId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create a workspace with a user Id " +
                        $"different from his user Id in claims{Environment.NewLine} " +
                        $"User id from request: {request.DirectorUserId}{Environment.NewLine} " +
                        $"User id from claims: {UserId}.");
                    return response.WithError(WorkSpaceErrorCodes.AccessDenied);
                }
                var mapRes = _mapper.Map<WorkSpace>(request);
                var result = await _workSpaceService.CreateOrEdit(mapRes);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to add or change element.{NewLine}{Parameter}:{Request}{NewLine2}",
                   Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return response.WithError(WorkSpaceErrorCodes.CannotCreateOrEditWorkspace);
            }
        }

        [Route("getUserInvitations")]
        [HttpGet]
        public async Task<DataResponse<List<UserWspStatusChangeModel>>> GetUserInvitations()
        {
            var response = new DataResponse<List<UserWspStatusChangeModel>>();

            try
            {
                var result = await _workSpaceService.GetUserInvitations(UserId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<UserWspStatusChangeModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting my workspaces => {Parameter1}: {UserId},", nameof(UserId), UserId);
                return response.WithError(WorkSpaceErrorCodes.CannotGetWpsRequests);
            }
        }

        [Route("createWspInvite")]
        [HttpPost]
        public async Task<DataResponse<bool>> CreateWspInvite(CreateWspInvitePostRequest request)
        {
            var response = new DataResponse<bool>();

            var isSuccess = await CheckRoles(nameof(CreateWspInvite));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {
                if (request.InviterId != UserId) 
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create a invite with a inviter Id " +
                       $"different from his user Id in claims{Environment.NewLine} " +
                       $"Inviter id from request: {request.InviterId}{Environment.NewLine} " +
                       $"User id from claims: {UserId}.");
                    return response.WithError(WorkSpaceErrorCodes.AccessDenied);
                }

                var mapRes = _mapper.Map<UserWorkspaceStatusChangeRequest>(request);
                var result = await _workSpaceService.CreateWpsInvitationRequest(mapRes);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to create workspace invite.{NewLine}{Parameter}:{Request}{NewLine2}",
                   Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return response.WithError(WorkSpaceErrorCodes.CannotCreateOrEditInviteWsp);
            }
        }
    }
}
