using Microsoft.AspNetCore.Mvc;

namespace TaskTracker.Controllers.BaseControllers
{
    /// <summary>
    /// Базовый контроллер
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
    }
}
