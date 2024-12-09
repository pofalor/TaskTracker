using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Web.Api.Controllers.BaseControllers;

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


    }
}
