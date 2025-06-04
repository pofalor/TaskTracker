using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/workspace")]
    public class WorkspaceController : ProtectedApiController
    {
        private readonly ILogger<WorkspaceController> _logger;
        private readonly IWorkspaceService _workSpaceService;
        private readonly IMapper _mapper;
        private readonly ILogNotificatorService _logNotificatorService;

        public WorkspaceController(ILogger<WorkspaceController> logger, IWorkspaceService workSpaceService,
            IMapper mapper, IUserService userService, ILogNotificatorService logNotificatorService)
        {
            _logger = logger;
            _workSpaceService = workSpaceService;
            _mapper = mapper;
            _logNotificatorService = logNotificatorService;
        }

        [Route("getMyWorkspaces")]
        [HttpGet]
        public async Task<DataResponse<List<WorkspaceModel>>> GetMyWorkspaces()
        {
            var response = new DataResponse<List<WorkspaceModel>>();

            try
            {
                var result = await _workSpaceService.GetMyWorkspaces(UserId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<WorkspaceModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting my workspaces => {Parameter1}: {UserId},", nameof(UserId), UserId);
                return response.WithError(WorkspaceErrorCodes.CannotGetMyWorkspaces);
            }
        }


        [Route("add")]
        [HttpPost]
        public async Task<DataResponse<bool>> CreateOrEdit(CreateOrEditWorkspacePostRequest request)
        {
            var response = new DataResponse<bool>();

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
                    return response.WithError(WorkspaceErrorCodes.AccessDenied);
                }
                var mapRes = _mapper.Map<Workspace>(request);
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
                return response.WithError(WorkspaceErrorCodes.CannotCreateOrEditWorkspace);
            }
        }

        [Route("getUserInvitations")]
        [HttpGet]
        public async Task<DataResponse<List<WorkspaceInviteModel>>> GetUserInvitations()
        {
            var response = new DataResponse<List<WorkspaceInviteModel>>();

            try
            {
                var result = await _workSpaceService.GetUserInvitations(UserId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<WorkspaceInviteModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user invitation => {Parameter1}: {UserId},", nameof(UserId), UserId);
                return response.WithError(WorkspaceErrorCodes.CannotGetWpsRequests);
            }
        }

        [Route("createWspInvite")]
        [HttpPost]
        public async Task<DataResponse<bool>> CreateWspInvite(CreateWspInvitePostRequest request)
        {
            var response = new DataResponse<bool>();

            try
            {
                if (request.InviterId != UserId) 
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create a invite with a inviter Id " +
                       $"different from his user Id in claims{Environment.NewLine} " +
                       $"Inviter id from request: {request.InviterId}{Environment.NewLine} " +
                       $"User id from claims: {UserId}.");
                    return response.WithError(WorkspaceErrorCodes.AccessDenied);
                }

                var mapRes = _mapper.Map<WorkspaceInvite>(request);
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
                return response.WithError(WorkspaceErrorCodes.CannotCreateOrEditInviteWsp);
            }
        }

        [Route("searchUsersForInvite")]
        [HttpPost]
        public async Task<DataResponse<List<UserModel>>> SearchUsersForInvite(SearchUserForInvitePR request)
        {
            var response = new DataResponse<List<UserModel>>();

            try
            {
                if (!request.InviterId.HasValue)
                    request.InviterId = UserId;

                if (request.InviterId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to find a user for invite " +
                       $"different from his user Id in claims{Environment.NewLine} " +
                       $"Inviter id from request: {request.InviterId}{Environment.NewLine} " +
                       $"User id from claims: {UserId}.");
                    return response.WithError(WorkspaceErrorCodes.AccessDenied);
                }

                var isWspMember = await _workSpaceService.IsWorkspaceMember(request.InviterId.Value, request.WorkspaceId);

                if (!isWspMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The inviter is looking for a user to create a workspace for, " +
                        $"even though they are not a member of the workspace.{Environment.NewLine}" +
                      $"Inviter id: {request.InviterId}{Environment.NewLine} " +
                      $"Workspace id: {request.WorkspaceId}.");
                    return response.WithError(WorkspaceErrorCodes.AccessDenied);
                }

                var result = await _workSpaceService.SearchUsersForInvite(request);

                if (!result.Success)
                    return response.WithError(result.Errors[0]);

                var mapRes = result.Data.Select(x=> _mapper.Map<UserModel>(x)).ToList();

                return response.WithData(mapRes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to search user for invite.{NewLine}{Parameter}:{Request}{NewLine2}",
                   Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return response.WithError(WorkspaceErrorCodes.CannotFindUserForInvite);
            }
        }

        [Route("isUserWorkspaceOwner")]
        [HttpGet]
        public async Task<DataResponse<bool>> IsUserWorkspaceOwner(int workspaceId)
        {
            var response = new DataResponse<bool>();

            try
            {
                var isWspOwner = await _workSpaceService.IsWorkspaceOwner(UserId, workspaceId);
                return response.WithData(isWspOwner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to check is user owner.{NewLine}{Parameter}:{WorkspaceId}{NewLine2}",
                   Environment.NewLine, nameof(workspaceId), workspaceId, Environment.NewLine);
                return response.WithError(WorkspaceErrorCodes.CannotCheckOwner);
            }
        }

        [Route("getUserCreatedInvites")]
        [HttpGet]
        public async Task<DataResponse<List<WorkspaceInviteModel>>> GetUserCreatedInvites(int workspaceId)
        {
            var response = new DataResponse<List<WorkspaceInviteModel>>();

            try
            {
                var result = await _workSpaceService.GetUserCreatedInvites(UserId, workspaceId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<WorkspaceInviteModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user created invites => {Parameter1}: {UserId},", nameof(UserId), UserId);
                return response.WithError(WorkspaceErrorCodes.CannotGetWpsRequests);
            }
        }

        [Route("acceptInvitationRequest")]
        [HttpPost]
        public async Task<DataResponse<bool>> AcceptInvitationRequest(AcceptInvitePR request)
        {
            var response = new DataResponse<bool>();

            try
            {
                if (!request.UserId.HasValue)
                    request.UserId = UserId;

                if (request.UserId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to accept a invite with a user Id " +
                       $"different from his user Id in claims{Environment.NewLine} " +
                       $"User id from request: {request.UserId}{Environment.NewLine} " +
                       $"User id from claims: {UserId}.");
                    return response.WithError(WorkspaceErrorCodes.AccessDenied);
                }

                var result = await _workSpaceService.AcceptInvitationRequest(request);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to accept workspace invite.{NewLine}{Parameter}:{Request}{NewLine2}",
                   Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return response.WithError(WorkspaceErrorCodes.CannotAcceptInviteWsp);
            }
        }

        [Route("getWorkspacesForCheck")]
        [HttpGet]
        [Authorize(Roles = Permissions.Admin)]
        public async Task<DataResponse<List<WorkspaceModel>>> GetWorkspacesForCheck()
        {
            var response = new DataResponse<List<WorkspaceModel>>();

            try
            {
                var result = await _workSpaceService.GetWorkspacesForCheck(UserId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<WorkspaceModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting workspaces for admin => {Parameter1}: {UserId},", nameof(UserId), UserId);
                return response.WithError(WorkspaceErrorCodes.CannotGetWspForAdmin);
            }
        }

        [Route("changeWorkspaceReviewStatus")]
        [HttpPost]
        [Authorize(Roles = Permissions.Admin)]
        public async Task<DataResponse<bool>> ChangeWorkspaceReviewStatus(CreateOrEditWorkspacePostRequest request)
        {
            var response = new DataResponse<bool>();

            try
            {
                var mapRes = _mapper.Map<Workspace>(request);
                var result = await _workSpaceService.ChangeWorkspaceReviewStatus(mapRes);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing workspace review status => {Parameter1}: {Request},", nameof(request), request?.ToJson());
                return response.WithError(WorkspaceErrorCodes.CannotChangeReviewStatus);
            }
        }
    }
}
