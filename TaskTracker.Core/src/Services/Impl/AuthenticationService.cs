using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TaskTracker.Core.src.ConfigSectionModels;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Identity;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;
        private readonly ApplicationIdentityDbContext _identityDbContext;
        readonly IdentityConfiguration IdentityConfig;


        public AuthenticationService(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager, ILogger<AuthenticationService> logger, 
        IMapper mapper, ApplicationDbContext dbContext, IConfiguration config, 
        ApplicationIdentityDbContext identityDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _identityDbContext = identityDbContext;

            try
            {
                IdentityConfig = config.GetSection(IdentityConfiguration.IdentitySectionInConfig).Get<IdentityConfiguration>()
                ?? throw new InvalidOperationException($"Cannot get {IdentityConfiguration.IdentitySectionInConfig} section from config. " +
                $"Value is null.");
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "{ClassName} fatal error getting value from config!{NewLine}", 
                    nameof(AuthenticationService), Environment.NewLine);
                throw;
            }
            
        }

        public async Task<IDataResult<bool>> RegisterNewUser(CreateUserPostRequest user)
        {
            var result = new DataResult<bool>();
            try
            {
                if (user.FirstName.IsEmpty())
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
                    using(var transactionIdentity = await _identityDbContext.Database.BeginTransactionAsync())
                    {
                        var identityUser = _mapper.Map<IdentityUser>(user);

                        var creationResult = await _userManager.CreateAsync(identityUser, user.Password);

                        if (!creationResult.Succeeded)
                        {
                            await transaction.RollbackAsync();
                            await transactionIdentity.RollbackAsync();
                            return result.WithError(creationResult.Errors.First().Description);
                        }

                        var newUser = _mapper.Map<User>(user);
                        newUser.UserId = identityUser.Id;

                        await _dbContext.AddAsync(newUser);
                        await _dbContext.SaveChangesAsync();

                        await _userManager.AddToRoleAsync(identityUser, Permissions.UserRole);

                        await transactionIdentity.CommitAsync();
                    }

                    await transaction.CommitAsync();
                }
                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating user.{NewLine}{Parameter}: {User}{NewLine2}", 
                    Environment.NewLine, nameof(user), user?.ToJson(), Environment.NewLine);
                return result.WithError(AuthenticationErrorCodes.ErrorCreatingUser);
            }
        }

        public async Task<IDataResult<AuthorizationModel>> Authenticate(AuthenticatePostRequest user)
        {
            var result = new DataResult<AuthorizationModel>();
            try
            {
                var userFromDb = await _dbContext.Set<User>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.Email == user.Email)
                    .FirstOrDefaultAsync();

                if (userFromDb == null) 
                {
                    return result.WithError(AuthenticationErrorCodes.InvalidEmailOrPassword);
                }
                
                var identityUser = await _identityDbContext.Set<IdentityUser>()
                    .AsNoTracking()
                    .Where(x=> x.Id == userFromDb.UserId)
                    .SingleAsync();

                var res = await _signInManager.CheckPasswordSignInAsync(identityUser, user.Password, true);

                if (!res.Succeeded)
                {
                    return result.WithError(AuthenticationErrorCodes.InvalidEmailOrPassword);
                }

                var authorizationModel = new AuthorizationModel
                {
                    Token = await GenerateJwtAsync(userFromDb, identityUser)
                };

                return result.WithData(authorizationModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error.{NewLine}{Parameter}: {User}{NewLine2}", 
                    Environment.NewLine, nameof(user), user?.ToJson(), Environment.NewLine);
                return result.WithError(AuthenticationErrorCodes.InvalidEmailOrPassword);
            }
        }

        private async Task<string> GenerateJwtAsync(User userFromDb, IdentityUser identityUser)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IdentityConfig.TokenSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(CustomClaimNames.UserId, userFromDb.Id.ToString()),
            };

            var roles = await _identityDbContext.Set<IdentityUserRole<string>>()
                .AsNoTracking()
                .Where(x => x.UserId == userFromDb.UserId)
                .Join(
                    _identityDbContext.Set<IdentityRole>(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r)
                .ToArrayAsync();

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var userClaims = await _identityDbContext.Set<IdentityUserClaim<string>>()
                .AsNoTracking()
                .Where(x => x.UserId == userFromDb.UserId)
                .ToArrayAsync();

            foreach (var claim in userClaims)
            {
                claims.Add(new Claim(claim.ClaimType, claim.ClaimValue));
            }

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, identityUser.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.AuthTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, userFromDb.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = credentials,
                Issuer = IdentityConfig.TokenIssuer,
                Audience = IdentityConfig.TokenAudience
            };
            
            var handler = new JsonWebTokenHandler();

            string token = handler.CreateToken(tokenDescriptor);

            return token;
        }

    }
}
