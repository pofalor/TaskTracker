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

        public WorkSpaceController(ILogger<WorkSpaceController> logger, IWorkSpaceService workSpaceService,
            IMapper mapper, IUserService userService) : base(logger, workSpaceService, mapper, userService)
        {
            _logger = logger;
            _workSpaceService = workSpaceService;
            _mapper = mapper;
        }

        public override void InitRoles()
        {
            AddRole(nameof(CreateOrEdit), Permissions.UserRole);
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


    }
}
