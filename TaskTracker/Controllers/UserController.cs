using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/user")]
    public class UserController : ProtectedApiController
    {
        private readonly ILogger _logger;
        private readonly UserService _userService;
        private readonly IMapper _mapper;

        public UserController(ILogger logger, UserService userService,
            IMapper mapper)
        {
            _logger = logger;
            _userService = userService;
            _mapper = mapper;
        }

        [Route("getUser")]
        [HttpGet]
        public async Task<DataResponse<UserModel>> GetUser()
        {
            var response = new DataResponse<UserModel>();

            if (!ModelState.IsValid)
            {
                return response.AddModelStateError(ModelState);
            }

            try
            {
                var result = await _userService.GetUserById(UserId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sedning request to get user by id.");
                return response.WithError(UserErrorCodes.CannotGetUser);
            }
        }
    }
}
