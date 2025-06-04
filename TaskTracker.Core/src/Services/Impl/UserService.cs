using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services.Impl
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserService> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationIdentityDbContext _identityDbContext;
        private readonly IMapper _mapper;

        public UserService(ApplicationDbContext dbContext, ILogger<UserService> logger, UserManager<IdentityUser> userManager,
            ApplicationIdentityDbContext identityDbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userManager = userManager;
            _identityDbContext = identityDbContext;
            _mapper = mapper;
        }

        public async Task<IDataResult<UserModel>> GetUserById(int id)
        {
            var result = new DataResult<UserModel>();

            try
            {
                var user = await _dbContext.Set<User>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Id == id)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return result.WithError(UserErrorCodes.CannotGetUser);

                var identityUser = await GetIdentityUser(user.UserId);
                var userModel = _mapper.Map<UserModel>(user);
                userModel.Roles = await _userManager.GetRolesAsync(identityUser);
                return result.WithData(userModel);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while getting user.{NewLine}{Parameter}: {Id}{NewLine2}",
                    Environment.NewLine, nameof(id), id, Environment.NewLine);
                return result.WithError(UserErrorCodes.CannotGetUser);
            }
        }

        private async Task<IdentityUser> GetIdentityUser(string userId)
        {
            return await _identityDbContext.Set<IdentityUser>()
                    .AsNoTracking()
                    .Where(x => x.Id == userId)
                    .SingleAsync();
        }
    }
}
