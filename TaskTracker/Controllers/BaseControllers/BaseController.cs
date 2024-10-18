using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Services;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Controllers.BaseControllers
{
    public class BaseController<T, M, R, F> : ProtectedApiController
        where T : PersistentEntity
        where R : BasePostRequest
        where F : BaseFilter
    {
        private readonly ILogger _logger;
        private readonly IBaseService<T, F> _baseService;
        private readonly IMapper _mapper;
        private readonly SignInManager<IdentityUser> _signInManager;
        private Dictionary<string, List<string>> AccessDict = new Dictionary<string, List<string>>();

        public BaseController(ILogger logger, IBaseService<T, F> baseService, 
            IMapper mapper, SignInManager<IdentityUser> signInManager)
        {
            _logger = logger;
            _baseService = baseService;
            _mapper = mapper;
            _signInManager = signInManager;
        }

        protected void AddRole(string methodName, string role)
        {
            var item = AccessDict.Get(methodName, []);

            if (item == null) AccessDict[methodName] = [role];
            else item.Add(role);
        }

        protected async Task<bool> CheckRoles(string methodName)
        {
            try
            {
                var accessRoles = AccessDict.Get(methodName, []);
                //TODO: протестить!
                var user = await _signInManager.UserManager.GetUserAsync(User);
                if(user is null) return false;
                var myRoles = await _signInManager.UserManager.GetRolesAsync(user);
                var result = false;
                if (accessRoles != null) accessRoles.Foreach(x => result = myRoles.Contains(x) || result);
                else result = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    //TODO: протестить
                    nameof(BaseController<T, M, R, F>), nameof(CheckRoles), Environment.NewLine, 
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return false;
            }
        }

        [Route("getAll")]
        [HttpGet]
        public virtual async Task<DataResponse<List<M>>> GetAll()
        {
            var response = new DataResponse<List<M>>();
            var isSuccess = await CheckRoles(nameof(GetAll));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {
                var result = await _baseService.GetAll();
                if (result.Success)
                {
                    return response.WithData(result.Data.Select(x => _mapper.Map<M>(x)).ToList());
                }

                return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseController<T, M, R, F>), nameof(GetAll), Environment.NewLine, 
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return response.WithError(BaseErrorCodes.GetItemsError);
            }
        }

        [Route("getByFilter")]
        [HttpPost]
        public virtual async Task<DataResponse<List<M>>> GetByFilter(F filter)
        {
            var response = new DataResponse<List<M>>();

            if (!ModelState.IsValid)
            {
                return response.AddModelStateError(ModelState);
            }

            var isSuccess = await CheckRoles(nameof(GetByFilter));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            filter.UserId = UserId;

            var user = await _signInManager.UserManager.GetUserAsync(User);
            if (user is null) return response.WithError(BaseErrorCodes.GetItemsError);
            var userRoles = await _signInManager.UserManager.GetRolesAsync(user);
            filter.IsAdmin = userRoles.Contains(Permissions.Admin);

            try
            {
                var result = await _baseService.GetByFilter(filter);
                if (result.Success)
                {
                    return response.WithData(result.Data.Select(x => _mapper.Map<M>(x)).ToList());
                }

                return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseController<T, M, R, F>), nameof(GetByFilter), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return response.WithError(BaseErrorCodes.GetItemsError);
            }
        }

        [Route("getById/{itemId}")]
        [HttpGet]
        public virtual async Task<DataResponse<M>> GetById(int itemId)
        {
            var response = new DataResponse<M>();

            if (!ModelState.IsValid)
            {
                return response.AddModelStateError(ModelState);
            }

            var isSuccess = await CheckRoles(nameof(GetById));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {
                var result = await _baseService.GetById(itemId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(_mapper.Map<M>(result.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseController<T, M, R, F>), nameof(GetById), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return response.WithError(BaseErrorCodes.GetItemError);
            }
        }

        [Route("deleteById/{itemId}")]
        [HttpGet]
        public virtual async Task<DataResponse<bool>> DeleteById(int itemId)
        {
            var response = new DataResponse<bool>();

            if (!ModelState.IsValid)
            {
                return response.AddModelStateError(ModelState);
            }

            var isSuccess = await CheckRoles(nameof(DeleteById));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {
                var result = await _baseService.DeleteById(itemId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseController<T, M, R, F>), nameof(DeleteById), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return response.WithError(BaseErrorCodes.DeleteItemError);
            }
        }

        [Route("add")]
        [HttpPost]
        public virtual async Task<DataResponse<bool>> CreateOrEdit(R request)
        {
            var response = new DataResponse<bool>();

            if (!ModelState.IsValid)
            {
                return response.AddModelStateError(ModelState);
            }

            var isSuccess = await CheckRoles(nameof(CreateOrEdit));
            if (!isSuccess)
                return response.WithError(SystemErrorCodes.AccessDenied);

            try
            {
                var mapRes = _mapper.Map<T>(request);
                var result = await _baseService.CreateOrEdit(mapRes);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(result.Errors[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseController<T, M, R, F>), nameof(CreateOrEdit), Environment.NewLine,
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return response.WithError(BaseErrorCodes.CreateItemError);
            }
        }
    }
}