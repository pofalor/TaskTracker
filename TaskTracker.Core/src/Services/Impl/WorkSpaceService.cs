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

        public async Task<IDataResult<List<WorkSpaceMember>>> GetMyWorkspaces(int userId)
        {
            var result = new DataResult<List<WorkSpaceMember>>();

            try
            {
                //Вытаскиваем все организации, где юзер - член рабочего пространства
                //(здесь будут содержаться также организации, которые создал юзер,
                //т.к. на юзера создаётся WorkSpaceMember UserTeamRole - Owner)
                var workspaces = await _dbContext.Set<WorkSpaceMember>()
                    .AsNoTracking()
                    .Include(x=> x.WorkSpace)
                    .Where(x=> x.UserId == userId)
                    .Where(x=> x.UserStatus != UserWorkSpaceStatus.Deleted)
                    .Where(x => !x.IsDeleted)
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
            try
            {
                var newWorkSpace = new WorkSpace();

                var existingWorkSpace = await _dbContext.Set<WorkSpace>()
                        .Where(x => request.Id == x.Id)
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
                        else if (string.IsNullOrEmpty(request.INN))
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

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    if(existingWorkSpace == null)
                        await _dbContext.AddAsync(newWorkSpace);
                    await _dbContext.SaveChangesAsync();

                    //создаём сотрудника(владельца) для новой компании
                    if (existingWorkSpace == null)
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
                    await transaction.CommitAsync();
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

        public async Task<IDataResult<List<UserWorkspaceStatusChangeRequest>>> GetUserInvitations(int userId)
        {
            var result = new DataResult<List<UserWorkspaceStatusChangeRequest>>();

            try
            {
                var statusesNeedShow = new UserStatusChangeType[]
                {
                    UserStatusChangeType.Default, UserStatusChangeType.UserDeclined
                };

                //Вытаскиваем все запросы, в которые приглашают юзера
                var statusChanges = await _dbContext.Set<UserWorkspaceStatusChangeRequest>()
                    .AsNoTracking()
                    .Include(x => x.WorkSpace)
                    .Include(x=> x.WorkSpace.DirectorUser)
                    .Include(x=> x.Inviter)
                    .Where(x => x.UserId == userId)
                    .Where(x => statusesNeedShow.Contains(x.RequestStatus))
                    .Where(x => !x.IsDeleted)
                    .Where(x=> !x.IsHidden)
                    .OrderBy(x=> x.RequestStatus)
                    .ToListAsync();

                return result.WithData(statusChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user workspace invations.{NewLine}{Parameter}: {UserId}{NewLine2}",
                    Environment.NewLine, nameof(userId), userId, Environment.NewLine);
                return result.WithError(WorkSpaceErrorCodes.CannotGetWpsRequests);
            }
        }

        public async Task<IDataResult<List<UserWorkspaceStatusChangeRequest>>> GetUserCreatedInvites(int userId, int workspaceId)
        {
            var result = new DataResult<List<UserWorkspaceStatusChangeRequest>>();

            try
            {
                var statusesNeedShow = new UserStatusChangeType[]
                {
                    UserStatusChangeType.Default, 
                    UserStatusChangeType.UserConfirmed, 
                    UserStatusChangeType.UserDeclined
                };
                //TODO: добавить проверку в контроллер, что текущий юзер делает запрос
                //Вытаскиваем все запросы, в которых юзер - приглашающий
                var statusChanges = await _dbContext.Set<UserWorkspaceStatusChangeRequest>()
                    .AsNoTracking()
                    .Include(x => x.WorkSpace)
                    .Where(x => x.InviterId == userId)
                    .Where(x => statusesNeedShow.Contains(x.RequestStatus))
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.IsHidden)
                    .OrderBy(x => x.RequestStatus)
                    .ToListAsync();

                return result.WithData(statusChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user workspace invations.{NewLine}{Parameter}: {UserId}{NewLine2}",
                    Environment.NewLine, nameof(userId), userId, Environment.NewLine);
                return result.WithError(WorkSpaceErrorCodes.CannotGetWpsRequests);
            }
        }

        public async Task<IDataResult<bool>> CreateWpsInvitationRequest(UserWorkspaceStatusChangeRequest request)
        {
            var result = new DataResult<bool>();
            try
            {
                if (request.WorkSpaceId <= 0)
                {
                    return result.WithError(WorkSpaceErrorCodes.WorkspaceNotSet);
                }
                else if (request.UserId <= 0)
                {
                    return result.WithError(WorkSpaceErrorCodes.WpsInviteUserIdNotSet);
                }
                else if (request.InviterId <= 0)
                {
                    return result.WithError(WorkSpaceErrorCodes.WpsInviterIdNotSet);
                }
                else if (request.Date == DateTime.MinValue)
                {
                    return result.WithError(WorkSpaceErrorCodes.WpsInviteReqDateNotSet);
                }
                else if (request.Date < DateTime.UtcNow)
                {
                    return result.WithError(WorkSpaceErrorCodes.WpsInviteReqDateFuture);
                }

                //два активных реквеста не может быть
                var activeRequestForUserExists = await _dbContext.Set<UserWorkspaceStatusChangeRequest>()
                        .Where(x => !x.IsDeleted)
                        .Where(x=> x.UserId == request.UserId)
                        .Where(x=> x.RequestStatus == UserStatusChangeType.Default)
                        .AnyAsync();

                if(activeRequestForUserExists)
                    return result.WithError(WorkSpaceErrorCodes.ActiveInviteAlreadyExists);

                var workspaceExists = await _dbContext.Set<WorkSpace>()
                        .Where(x => !x.IsDeleted)
                        .Where(x => x.DirectorUserId == request.InviterId)
                        .Where(x => x.Id == request.WorkSpaceId)
                        .AnyAsync();

                if (!workspaceExists)
                    return result.WithError(WorkSpaceErrorCodes.WpsForInviteNotExists);

                 var lastUserMemberInfo = await _dbContext.Set<WorkSpaceMember>()
                        .Where(x => !x.IsDeleted)
                        .Where(x => x.UserId == request.UserId)
                        .Where(x => x.WorkSpaceId == request.WorkSpaceId)
                        .OrderBy(x=> x.Id)
                        .FirstOrDefaultAsync();

                var lastUserMemberInfoExists = lastUserMemberInfo != null;
                if (lastUserMemberInfoExists && lastUserMemberInfo.UserStatus == request.NewStatus)
                    return result.WithError(WorkSpaceErrorCodes.UserAlreadyInWsp);

                var newWpsInviteRequest = new UserWorkspaceStatusChangeRequest()
                { 
                    UserId = request.UserId,
                    WorkSpaceId = request.WorkSpaceId,
                    InviterId = request.InviterId,
                    Date = request.Date,
                    PreviousStatus = lastUserMemberInfo?.UserStatus,
                    NewStatus = request.NewStatus,
                    RequestStatus = UserStatusChangeType.Default
                };

                await _dbContext.AddAsync(newWpsInviteRequest);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating invite to WSP.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(WorkSpaceErrorCodes.CannotCreateOrEditInviteWsp);
            }
        }
    }
}
