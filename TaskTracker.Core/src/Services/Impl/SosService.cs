using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class SosService : ISosService
    {
        readonly ILogger<SosService> _logger;
        readonly ApplicationDbContext _dbContext;
        readonly RoleManager<IdentityRole> _roleManager;
        readonly ApplicationIdentityDbContext _identityDbContext;

        public SosService(ILogger<SosService> logger, ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager,
            ApplicationIdentityDbContext identityDbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _roleManager = roleManager;
            _identityDbContext = identityDbContext;
        }

        public async Task<IDataResult<bool>> CreateNewRole(string roleName)
        {
            var result = new DataResult<bool>();
            try
            {
                if (roleName.IsEmpty())
                {
                    return result.WithError(SosErrorCodes.RoleNameNullError);
                }

                var roleExists = await _identityDbContext.Set<IdentityRole>()
                    .AsNoTracking()
                    .Where(x => x.Name == roleName)
                    .AnyAsync();

                if (roleExists)
                {
                    return result.WithError(SosErrorCodes.RoleAlreadyExists);
                }

                var role = new IdentityRole(roleName);
                var res = await _roleManager.CreateAsync(role);

                if (res.Succeeded)
                {
                    return result.WithData(true);
                }
                else
                {
                    return result.WithError(res.Errors.First().Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating role.{NewLine}{Parameter}: {RoleName}{NewLine2}",
                    Environment.NewLine, nameof(roleName), roleName, Environment.NewLine);
                return result.WithError(SosErrorCodes.RoleCreationError);
            }
        }
    }
}
