using Microsoft.AspNetCore.Mvc;
using TaskTracker.Web.Api.Attributes;

namespace TaskTracker.Controllers.BaseControllers
{
    /// <summary>
    /// Базовый контроллер
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiRequestValidation]
    public abstract class BaseApiController : ControllerBase
    {
    }
}
