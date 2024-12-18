using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class WorkSpaceService : BaseService<WorkSpace, WorkSpaceFilter>, IWorkSpaceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<WorkSpaceService> _logger;

        public WorkSpaceService(ApplicationDbContext dbContext, ILogger<WorkSpaceService> logger) :
            base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IDataResult<List<WorkSpace>>> GetMyWorkspaces(int userId)
        {
            var result = new DataResult<List<WorkSpace>>();

            try
            {
                //Вытаскиваем все организации, где юзер - член рабочего пространства
                //(здесь будут содержаться также организации, которые создал юзер,
                //т.к. на юзера создаётся WorkSpaceMember UserTeamRole - Owner)
                var workspaces = await _dbContext.Set<WorkSpaceMember>()
                    .AsNoTracking()
                    .Where(x=> x.UserId == userId)
                    .Where(x=> x.UserStatus != UserWorkSpaceStatus.Deleted)
                    .Where(x => !x.IsDeleted)
                    .Select(x=> x.WorkSpace)
                    .ToListAsync();

                return result.WithData(workspaces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting my workspaces.{NewLine}{Parameter}: {UserId}{NewLine2}",
                    Environment.NewLine, nameof(userId), userId, Environment.NewLine);
                return result.WithError(WorkSpaceErrorCodes.CannotGetMyWorkspaces);
            }
        }

        public override async Task<IDataResult<bool>> CreateOrEdit(WorkSpace request)
        {
            var result = new DataResult<bool>();
            //TODO: в контроллере проверить, что юзер текущий делает запрос
            try
            {
                var newWorkSpace = new WorkSpace();

                var existingWorkSpace = await _dbContext.Set<WorkSpace>()
                        .AsNoTracking()
                        .WhereIf(request.Id != 0, x => request.Id == x.Id)
                        .Where(x => !x.IsDeleted)
                        .FirstOrDefaultAsync();

                //если не пусто, значит изменяем, иначе добавляем новое рабочее пространство
                if (existingWorkSpace != null) 
                {
                    newWorkSpace = existingWorkSpace;
                }
                else
                {
                    if (request.WorkSpaceType == WorkSpaceType.Company)
                    {
                        if (!request.Country.HasValue)
                        {
                            return result.WithError(WorkSpaceErrorCodes.CountryNull);
                        }
                        else if (!request.INN.HasValue)
                        {
                            return result.WithError(WorkSpaceErrorCodes.INNNull);
                        }
                        else if (string.IsNullOrEmpty(request.Address))
                        {
                            return result.WithError(WorkSpaceErrorCodes.AddressNull);
                        }
                        else if (!request.RegistrationDate.HasValue)
                        {
                            return result.WithError(WorkSpaceErrorCodes.RegistrationDateNull);
                        }
                        //У рабочего пространства компании должно быть уникальное имя(в разрезе управляющего,
                        //т.е. один управляющий может создавать рабочие пространства только с разными именами)
                        var workspaceWithSameName = await _dbContext.Set<WorkSpace>()
                            .AsNoTracking()
                            .Where(x => x.Name == request.Name)
                            .Where(x => x.DirectorUserId == request.DirectorUserId)
                            .Where(x => x.WorkSpaceType == request.WorkSpaceType)
                            .Where(x => !x.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (workspaceWithSameName == null)
                        {
                            //Не должно быть рабочих пространств с одинаковыми инн, не должно быть одинаковых комбинаций:
                            //даты регистрации, юр. адреса, страны.
                            var existingWorkspaceByCompanyFields = await _dbContext.Set<WorkSpace>()
                                .AsNoTracking()
                                .Where(x => x.INN == request.INN
                                || (x.Country == request.Country && x.Address == request.Address && x.RegistrationDate == request.RegistrationDate))
                                .Where(x => x.WorkSpaceType == request.WorkSpaceType)
                                .Where(x => !x.IsDeleted)
                                .FirstOrDefaultAsync();

                            if (existingWorkspaceByCompanyFields != null)
                            {
                                var errorCode = request.INN == existingWorkspaceByCompanyFields.INN ? WorkSpaceErrorCodes.CompanyWithInnAlreadyExists
                                    : WorkSpaceErrorCodes.CompanyWithDataAlreadyExists;
                                return result.WithError(errorCode);
                            }
                        }
                        else
                        {
                            return result.WithError(WorkSpaceErrorCodes.CompanyWithNameAlreadyExists);
                        }
                    }
                    else if (request.WorkSpaceType == WorkSpaceType.Personal)
                    {
                        var existsPersonalWorkspace = await _dbContext.Set<WorkSpace>()
                            .AsNoTracking()
                            .Where(x => x.WorkSpaceType == request.WorkSpaceType)
                            .Where(x => x.DirectorUserId == request.DirectorUserId)
                            .Where(x => !x.IsDeleted)
                            .AnyAsync();

                        if (existsPersonalWorkspace)
                        {
                            return result.WithError(WorkSpaceErrorCodes.CanCreateOnlyOnePersonalWorkspace);
                        }
                    }
                    newWorkSpace.WorkSpaceType = request.WorkSpaceType;
                    newWorkSpace.DirectorUserId = request.DirectorUserId;
                }
                //TODO: сделать через автомаппер
                newWorkSpace.Name = request.Name;
                newWorkSpace.Country = request.Country;
                newWorkSpace.RegistrationDate = request.RegistrationDate;
                newWorkSpace.Address = request.Address;
                newWorkSpace.INN = request.INN;

                await _dbContext.AddAsync(newWorkSpace);
                await _dbContext.SaveChangesAsync();
                
                //создаём сотрудника(владельца) для новой компании
                if(existingWorkSpace == null)
                {
                    var newWorkSpaceMember = new WorkSpaceMember()
                    {
                        UserId = newWorkSpace.DirectorUserId,
                        TeamRole = UserTeamRole.Owner,
                        UserStatus = UserWorkSpaceStatus.Active, 
                        WorkSpaceId = newWorkSpace.Id
                    };

                    await _dbContext.AddAsync(newWorkSpaceMember);
                    await _dbContext.SaveChangesAsync();
                }

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating or changing workspace.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(WorkSpaceErrorCodes.CannotCreateOrEditWorkspace);
            }
        }
    }
}
