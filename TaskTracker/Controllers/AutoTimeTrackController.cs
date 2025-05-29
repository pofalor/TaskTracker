using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/autotrack")]
    public class AutoTimeTrackController : ProtectedApiController
    {
        private readonly ILogger<AutoTimeTrackController> _logger;
        private readonly IAutoTimeTrackService _autoTimeTrackService;
        private readonly IMapper _mapper;
        private readonly ILogNotificatorService _logNotificatorService;
        private readonly IWorkspaceService _workSpaceService;

        public AutoTimeTrackController(ILogger<AutoTimeTrackController> logger, IAutoTimeTrackService autoTimeTrackService,
            IMapper mapper, ILogNotificatorService logNotificatorService, IWorkspaceService workSpaceService)
        {
            _logger = logger;
            _autoTimeTrackService = autoTimeTrackService;
            _mapper = mapper;
            _logNotificatorService = logNotificatorService;
            _workSpaceService = workSpaceService;
        }

        [Route("getActiveAutoTrack")]
        [HttpGet]
        public async Task<DataResponse<TimeTrackingModel?>> GetActiveAutoTrack(int projectId, int workspaceId)
        {
            var response = new DataResponse<TimeTrackingModel?>();

            try
            {
                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(UserId, workspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to get active auto track, " +
                        $"but he not workspace membership{Environment.NewLine} " +
                        $"Project id: {projectId}{Environment.NewLine} " +
                        $"User id: {UserId}.");
                    return response.WithError(AutoTimeTrackErrorCodes.UserNotMemberWsp);
                }

                var result = await _autoTimeTrackService.GetActiveAutoTrack(UserId, projectId);


                if (result.Success)
                {
                    var mapRes = _mapper.Map<TimeTrackingModel>(result.Data);
                    return response.WithData(mapRes);
                }
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to get auto track time.{NewLine}" +
                    "{Parameter}:{UserId}{NewLine2}" +
                    "{Parameter2}:{ProjectId}",
                   Environment.NewLine, nameof(UserId), UserId, Environment.NewLine, nameof(projectId), projectId);
                return response.WithError(AutoTimeTrackErrorCodes.CannotGetAutoTrack);
            }
        }

        [Route("startTracking")]
        [HttpPost]
        public async Task<DataResponse<TimeTrackingModel>> StartTracking(AutoTimeTrackPR request)
        {
            var response = new DataResponse<TimeTrackingModel>();

            try
            {
                if (!request.UserId.HasValue)
                    request.UserId = UserId;

                if (request.UserId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request for auto tracking " +
                        $"different from his user Id in claims{Environment.NewLine} " +
                        $"User id from request: {request.UserId}{Environment.NewLine} " +
                        $"User id from claims: {UserId}.");
                    return response.WithError(AutoTimeTrackErrorCodes.AccessDenied);
                }

                var mapResReq = _mapper.Map<TimeTracking>(request);
                var result = await _autoTimeTrackService.StartTracking(mapResReq);


                if (result.Success)
                {
                    var mapRes = _mapper.Map<TimeTrackingModel>(result.Data);
                    return response.WithData(mapRes);
                }
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to start auto tracking.{NewLine}" +
                    "{Parameter}:{Request}",
                   Environment.NewLine, nameof(request), request?.ToJson());
                return response.WithError(AutoTimeTrackErrorCodes.CannotStartAutoTrack);
            }
        }

        [Route("stopTracking")]
        [HttpPost]
        public async Task<DataResponse<TimeTrackingModel>> StopTracking(AutoTimeTrackPR request)
        {
            var response = new DataResponse<TimeTrackingModel>();

            try
            {
                if (!request.UserId.HasValue)
                    request.UserId = UserId;

                if (request.UserId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request for stop auto tracking " +
                        $"different from his user Id in claims{Environment.NewLine} " +
                        $"User id from request: {request.UserId}{Environment.NewLine} " +
                        $"User id from claims: {UserId}.");
                    return response.WithError(AutoTimeTrackErrorCodes.AccessDenied);
                }

                var mapResReq = _mapper.Map<TimeTracking>(request);
                var result = await _autoTimeTrackService.StopTracking(mapResReq);


                if (result.Success)
                {
                    var mapRes = _mapper.Map<TimeTrackingModel>(result.Data);
                    return response.WithData(mapRes);
                }
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to start auto tracking.{NewLine}" +
                    "{Parameter}:{Request}",
                   Environment.NewLine, nameof(request), request?.ToJson());
                return response.WithError(AutoTimeTrackErrorCodes.CannotStartAutoTrack);
            }
        }
    }
}
