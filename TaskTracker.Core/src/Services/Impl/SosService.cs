using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class SosService : ISosService
    {
        readonly ILogger<SosService> _logger;
        readonly ApplicationDbContext _dbContext;
        readonly RoleManager<IdentityRole> _roleManager;
        readonly UserManager<IdentityUser> _userManager;
        readonly ApplicationIdentityDbContext _identityDbContext;

        public SosService(ILogger<SosService> logger, ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager,
            ApplicationIdentityDbContext identityDbContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _roleManager = roleManager;
            _identityDbContext = identityDbContext;
            _userManager = userManager;
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

        public async Task<IDataResult<bool>> SetToRole(string roleName, int userId)
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

                if (!roleExists)
                {
                    return result.WithError(SosErrorCodes.RoleNotExists);
                }

                var userFromDb = await _dbContext.Set<User>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.Id == userId)
                    .FirstOrDefaultAsync();

                if (userFromDb == null)
                {
                    return result.WithError(SosErrorCodes.UserNotExists);
                }

                var identityUser = await _identityDbContext.Set<IdentityUser>()
                    .Where(x => x.Id == userFromDb.UserId)
                    .SingleAsync();

                var res = await _userManager.AddToRoleAsync(identityUser, roleName);

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
                _logger.LogError(ex, "Error while adding user to role.{NewLine}{Parameter}: {RoleName}{NewLine2}{Parameter2}: {UserId}",
                    Environment.NewLine, nameof(roleName), roleName, Environment.NewLine, nameof(userId), userId);
                return result.WithError(SosErrorCodes.RoleAddingError);
            }
        }
    }
}
