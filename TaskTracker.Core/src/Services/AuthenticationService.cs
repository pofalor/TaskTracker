using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Identity;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services
{
    public class AuthenticationService 
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;

        public AuthenticationService(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager, ILogger logger, IMapper mapper, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<IDataResult<bool>> RegisterNewUser(CreateUserPostRequest user)
        {
            var result = new DataResult<bool>();
            try
            {
                if(user.FirstName.IsEmpty())
                {
                    return result.WithError(AuthenticationErrorCodes.ErrorFirstName);
                }
                else if (user.LastName.IsEmpty()) 
                {
                    return result.WithError(AuthenticationErrorCodes.ErrorLastName);
                }
                if (user.Email.IsEmpty() || !user.Email.IsEmail()) 
                {
                    return result.WithError(AuthenticationErrorCodes.InvalidEmail);
                }

                var userExists = await _dbContext.Set<User>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.Email == user.Email)
                    .AnyAsync();

                if (userExists)
                {
                    return result.WithError(AuthenticationErrorCodes.UserAlreadyExists);
                }

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    var identityUser = _mapper.Map<IdentityUser>(user);

                    var creationResult = await _userManager.CreateAsync(identityUser, user.Password);

                    if (!creationResult.Succeeded)
                    {
                        //TODO: протестить
                        await transaction.RollbackAsync();
                        return result.WithError(AuthenticationErrorCodes.ErrorCreatingUser);
                    }

                    var newUser = _mapper.Map<User>(user);
                    newUser.UserId = identityUser.Id;

                    await _dbContext.AddAsync(newUser);
                    await _dbContext.SaveChangesAsync();

                    await _userManager.AddToRoleAsync(identityUser, Permissions.UserRole);

                    await transaction.CommitAsync();
                }    
                return result.WithData(true);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "{ClassName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(AuthenticationService), nameof(RegisterNewUser), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(AuthenticationErrorCodes.ErrorCreatingUser);
            }
        }

        public async Task<IDataResult<bool>> Authenticate(AuthenticatePostRequest user)
        {
            var result = new DataResult<bool>();
            try
            {
                var userFromDb = await _dbContext.Set<User>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.Email == user.Email)
                    .FirstOrDefaultAsync();

                var claims = new List<Claim> { new(ClaimTypes.Name, username), new (CustomClaimNames.UserId, userFromDb.Id) };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var res = await _signInManager.PasswordSignInAsync(user.Email, user.Password, false, true);
                //TODO: отдать токен
                if (res.Succeeded)
                {
                    return result.WithData(true);
                }
                return result.WithError(AuthenticationErrorCodes.InvalidEmailOrPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ClassName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(AuthenticationService), nameof(Authenticate), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return result.WithError(AuthenticationErrorCodes.InvalidEmailOrPassword);
            }
        }

    }
}
