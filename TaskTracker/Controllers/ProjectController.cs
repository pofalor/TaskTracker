using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/project")]
    public class ProjectController : ProtectedApiController
    {
        private readonly ILogger<ProjectController> _logger;
        private readonly IWorkspaceService _workSpaceService;
        private readonly IProjectService _projectService;
        private readonly IMapper _mapper;
        private readonly ILogNotificatorService _logNotificatorService;

        public ProjectController(ILogger<ProjectController> logger, IWorkspaceService workSpaceService,
            IMapper mapper, IUserService userService, ILogNotificatorService logNotificatorService, IProjectService projectService) 
        {
            _logger = logger;
            _workSpaceService = workSpaceService;
            _mapper = mapper;
            _logNotificatorService = logNotificatorService;
            _projectService = projectService;
        }

        [Route("getWorkspaceProjects")]
        [HttpGet]
        public async Task<DataResponse<List<ProjectModel>>> GetWorkspaceProjects(int workspaceId)
        {
            var response = new DataResponse<List<ProjectModel>>();

            try
            {
                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(UserId, workspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to get a workspace projects," +
                        $"but he not workspace membership{Environment.NewLine} " +
                        $"Workspace id: {workspaceId}{Environment.NewLine} " +
                        $"User id: {UserId}.");
                    return response.WithError(ProjectErrorCodes.UserNotMemberWsp);
                }

                var result = await _projectService.GetWorkspaceProjects(workspaceId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<ProjectModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting projects => {Parameter1}: {WorkspaceId},", nameof(workspaceId), workspaceId);
                return response.WithError(ProjectErrorCodes.CannotGetProjects);
            }
        }

        [Route("add")]
        [HttpPost]
        public async Task<DataResponse<bool>> CreateOrEdit(CreateOrEditProjectPR request)
        {
            var response = new DataResponse<bool>();

            try
            {
                if (!request.AuthorId.HasValue)
                    request.AuthorId = UserId;

                if (request.AuthorId != UserId)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create a project with a user Id " +
                        $"different from his user Id in claims{Environment.NewLine} " +
                        $"User id from request: {request.AuthorId}{Environment.NewLine} " +
                        $"User id from claims: {UserId}.");
                    return response.WithError(ProjectErrorCodes.AccessDenied);
                }

                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(UserId, request.WorkspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to create a project" +
                        $"but he not workspace membership{Environment.NewLine} " +
                        $"Workspace id: {request.WorkspaceId}{Environment.NewLine} " +
                        $"User id: {UserId}.");
                    return response.WithError(ProjectErrorCodes.UserNotMemberWsp);
                }

                var mapRes = _mapper.Map<Project>(request);
                var result = await _projectService.CreateOrEdit(mapRes);

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

        [Route("getProjectMgrCandidates")]
        [HttpGet]
        public async Task<DataResponse<List<UserModel>>> GetProjectMgrCandidates(int workspaceId)
        {
            var response = new DataResponse<List<UserModel>>();

            try
            {
                var isWorkspaceMember = await _workSpaceService.IsWorkspaceMember(UserId, workspaceId);
                if (!isWorkspaceMember)
                {
                    await _logNotificatorService.SendTelegramAdminAsync($"The user has sent a request to get project manager candidates, " +
                        $"but he not workspace membership{Environment.NewLine} " +
                        $"Workspace id: {workspaceId}{Environment.NewLine} " +
                        $"User id: {UserId}.");
                    return response.WithError(ProjectErrorCodes.UserNotMemberWsp);
                }

                var result = await _projectService.GetProjectMgrCandidates(workspaceId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                var models = result.Data.Select(x => _mapper.Map<UserModel>(x)).ToList();
                return response.WithData(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting project manager candidates => {Parameter1}: {WorkspaceId},", nameof(workspaceId), workspaceId);
                return response.WithError(ProjectErrorCodes.CannotGetProjectMgrCandidates);
            }
        }
    }
}
