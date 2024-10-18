using Microsoft.AspNetCore.Mvc;

namespace TaskTracker.Controllers.BaseControllers
{
    /// <summary>
    /// Базовый контроллер
    /// </summary>
    [Route("api/[controller]")]
    public abstract class BaseApiController : Controller
    {
    }
}
