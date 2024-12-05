using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Core.src.Services;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : BaseApiController
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IAuthenticationService _authenticationService;
        public AuthenticationController(ILogger<AuthenticationController> logger, IAuthenticationService authenticationService) 
        {
            _logger = logger;
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public async Task<DataResponse<bool>> Register(CreateUserPostRequest user)
        {
            var result = new DataResponse<bool>();
            try
            {
                var response = await _authenticationService.RegisterNewUser(user);
                if (response.Success && response.Data)
                {
                   return result.WithData(response.Data);
                }
                    
                return result.WithError(response.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to create new user.{NewLine}{Parameter}: {User}{NewLine2}",
                    Environment.NewLine, nameof(user), user?.ToJson(), Environment.NewLine);
                return result.WithError(AuthenticationErrorCodes.ErrorCreatingUser);
            }
        }

        [HttpPost("authenticate")]
        public async Task<DataResponse<AuthorizationModel>> AuthUser(AuthenticatePostRequest user)
        {
            var result = new DataResponse<AuthorizationModel>();
            try
            {
                var response = await _authenticationService.Authenticate(user);
                if (response.Success)
                    return result.WithData(response.Data);
                return result.WithError(response.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to authenticate.{NewLine}{Parameter}: {User}{NewLine2}",
                    Environment.NewLine, nameof(user), user?.ToJson(), Environment.NewLine);
                return result.WithError(AuthenticationErrorCodes.AuthError);
            }
        }
    }
}
