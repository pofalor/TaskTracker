using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Controllers.BaseControllers;
using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Web.Api.Controllers.BaseControllers
{
    public class BaseController<T, M, R, F, L> : ProtectedApiController
        where T : PersistentEntity
        where R : BasePostRequest
        where F : BaseFilter
    {
        private readonly ILogger<L> _logger;
        private readonly IBaseService<T, F> _baseService;
        private readonly IMapper _mapper;
        private Dictionary<string, List<string>> AccessDict = new Dictionary<string, List<string>>();

        public BaseController(ILogger<L> logger, IBaseService<T, F> baseService, IMapper mapper)
        {
            _logger = logger;
            _baseService = baseService;
            _mapper = mapper;
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
                var myRoles = await UserManager.GetRolesAsync(UserId);
                var result = false;
                if (accessRoles != null) accessRoles.Foreach(x => result = myRoles.Contains(x) || result);
                else result = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    //TODO: протестить
                    nameof(BaseController<T, M, R, F, L>), nameof(CheckRoles), Environment.NewLine, 
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                return false;
            }
        }

        [Route("getAll")]
        [HttpGet]
        public virtual async Task<DataResponse<List<M>>> GetAll()
        {
            var response = new DataResponse<List<M>>();
            var isSuccess = await CheckRoles("GetAll");
            if (!isSuccess)
                return response.WithError(206, "Access denied");

            try
            {
                var result = await _baseService.GetAll();
                if (result.Success)
                {
                    return response.WithData(result.Data.Select(x => _mapper.Map<M>(x)).ToList());
                }

                return response.WithError(517, "Cannot get items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ControllerName} {MethodName}(){NewLine}. Msg: {Message}{StackTrace}{InnerException}",
                    nameof(BaseController<T, M, R, F, L>), nameof(GetAll), Environment.NewLine, 
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);
                //TODO: сделать через енам
                return response.WithError(517, "Cannot get items");
            }
        }

        [Route("getByFilter")]
        [HttpPost]
        public virtual async Task<DataResponse<List<M>>> GetByFilter(F filter)
        {
            var response = new DataResponse<List<M>>();
            var isSuccess = await CheckRoles("GetByFilter");
            if (!isSuccess)
                return response.WithError(206, "Access denied");

            filter.CryptoUserId = CryptoUserId;
            var userRoles = await UserManager.GetRolesAsync(UserId);
            filter.IsAdmin = userRoles.Contains(Permissions.Admin);

            try
            {
                var result = await _baseService.GetByFilter(filter);
                if (result.Success)
                {
                    return response.WithData(result.Data.Select(x => _mapper.Map<M>(x)).ToList());
                }

                return response.WithError(517, "Cannot get items");
            }
            catch (Exception e)
            {
                _logger.SendTelegram($"Base GetByFilter() ->\r\n{e.Message}{e.StackTrace}{e.InnerException?.Message}", e);
                return response.WithError(517, "Cannot get items");
            }
        }

        [Route("getById/{itemId}")]
        [HttpGet]
        public virtual async Task<DataResponse<M>> GetById(int itemId)
        {
            var response = new DataResponse<M>();
            var isSuccess = await CheckRoles("GetById");
            if (!isSuccess)
                return response.WithError(206, "Access denied");

            try
            {
                var result = await _baseService.GetById(itemId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(_mapper.Map<M>(result.Data));
            }
            catch (Exception e)
            {
                _logger.SendTelegram($"Base GetById() ->\r\n{e.Message}{e.StackTrace}{e.InnerException?.Message}", e);
                return response.WithError(517, "Cannot get item");
            }
        }

        [Route("deleteById/{itemId}")]
        [HttpGet]
        public virtual async Task<DataResponse<bool>> DeleteById(int itemId)
        {
            var response = new DataResponse<bool>();
            var isSuccess = await CheckRoles("DeleteById");
            if (!isSuccess)
                return response.WithError(206, "Access denied");

            try
            {
                var result = await _baseService.DeleteById(itemId);
                if (!result.Success)
                {
                    return response.WithError(result.Errors[0]);
                }

                return response.WithData(result.Data);
            }
            catch (Exception e)
            {
                _logger.SendTelegram($"Base DeleteById() ->\r\n{e.Message}{e.StackTrace}{e.InnerException?.Message}", e);
                return response.WithError(203, "Cannot delete item");
            }
        }

        [Route("add")]
        [HttpPost]
        public virtual async Task<DataResponse<bool>> CreateOrEdit(R request)
        {
            var response = new DataResponse<bool>();
            var isSuccess = await CheckRoles("CreateOrEdit");
            if (!isSuccess)
                return response.WithError(206, "Access denied");

            try
            {
                var mapRes = _mapper.Map<T>(request);
                var result = await _baseService.CreateOrEdit(mapRes);

                if (result.Success)
                    return response.WithData(result.Data);
                else
                    return response.WithError(201, "Cannot create item");
            }
            catch (Exception e)
            {
                _logger.SendTelegram($"Base CreateOrEdit() ->\r\n{e.Message}{e.StackTrace}{e.InnerException?.Message}", e);
                return response.WithError(365, "Cannot create item");
            }
        }
    }
}