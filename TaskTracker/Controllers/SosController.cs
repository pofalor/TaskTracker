using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.ConfigSectionModels;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Services;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers
{
    [Route("api/sos")]
    [ApiController]
    public class SosController : BaseApiController
    {
        private readonly ILogger<SosController> _logger;
        private readonly ISosService _sosService;
        readonly string AnonymousTokenRequest;
        public SosController(ILogger<SosController> logger, ISosService sosService, IConfiguration config)
        {
            _logger = logger;
            _sosService = sosService;

            try
            {
                AnonymousTokenRequest = config
                    .GetSection(SecurityConfiguration.SecuritySectionInConfig)
                    .Get<SecurityConfiguration>()?.AnonymousTokenRequest
                    ?? throw new InvalidOperationException($"Cannot get {SecurityConfiguration.SecuritySectionInConfig} section, " +
                        $"{nameof(SecurityConfiguration.AnonymousTokenRequest)} key from config. " +
                        $"Value is null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} fatal error getting value from config!{NewLine}",
                    nameof(SosController), Environment.NewLine);
                throw;
            }
        }

        [HttpGet("createnewrole")]
        public async Task<DataResponse<bool>> CreateNewRole(string roleName, string securityToken)
        {
            var result = new DataResponse<bool>();
            try
            {
                if (securityToken != AnonymousTokenRequest)
                {
                    return result.WithError(SosErrorCodes.InvalidToken);
                }

                var response = await _sosService.CreateNewRole(roleName);
                if (response.Success && response.Data)
                {
                    return result.WithData(response.Data);
                }

                return result.WithError(response.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending request to create new role.{NewLine}" +
                    "{Parameter1}: {RoleName}, {Parameter2}: {Token}{NewLine2}",
                    Environment.NewLine, nameof(roleName), roleName, nameof(securityToken), securityToken, Environment.NewLine);
                return result.WithError(SosErrorCodes.RoleCreationError);
            }
        }
    }
}
