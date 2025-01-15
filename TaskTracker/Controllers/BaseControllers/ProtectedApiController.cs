using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Identity;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Controllers.BaseControllers
{
    /// <summary>
    /// Защищенный авторизацией контроллер (пользователь должен быть аутентифицирован)
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public abstract class ProtectedApiController : BaseApiController
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        protected int UserId
        {
            get
            {
                var claim = HttpContext.User.FindFirst(CustomClaimNames.UserId);
                if (claim is not null)
                {
                    return claim.Value.ToInt();
                }
                return 0;
            }
        }
    }
}
