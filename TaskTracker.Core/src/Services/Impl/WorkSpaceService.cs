using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(ApplicationDbContext dbContext, ILogger<WorkspaceService> logger)
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
                return result.WithError(WorkspaceErrorCodes.CannotGetMyWorkspaces);
            }
        }

        public async Task<IDataResult<bool>> CreateOrEdit(WorkSpace request)
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
                            return result.WithError(WorkspaceErrorCodes.CountryNull);
                        }
                        else if (string.IsNullOrEmpty(request.INN))
                        {
                            return result.WithError(WorkspaceErrorCodes.INNNull);
                        }
                        else if (string.IsNullOrEmpty(request.Address))
                        {
                            return result.WithError(WorkspaceErrorCodes.AddressNull);
                        }
                        else if (!request.RegistrationDate.HasValue)
                        {
                            return result.WithError(WorkspaceErrorCodes.RegistrationDateNull);
                        }
                        else if (!request.ReviewStatus.HasValue)
                        {
                            _logger.LogError("Review status is null. UserId: {UserId}. WorkspaceName: {Name}. ReviewStatus: {Status}",
                                request.DirectorUserId, request.Name, request.ReviewStatus);
                            return result.WithError(WorkspaceErrorCodes.ReviewStatusNull);
                        }
                        else if (request.ReviewStatus != WorkspaceReviewStatus.OnReview)
                        {
                            _logger.LogError("The review status is different from what is required. UserId: {UserId}. " +
                                "WorkspaceName: {Name}. ReviewStatus: {Status}",
                                request.DirectorUserId, request.Name, request.ReviewStatus);
                            return result.WithError(WorkspaceErrorCodes.ReviewStatusWrong);
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
                                var errorCode = request.INN == existingWorkspaceByCompanyFields.INN ? WorkspaceErrorCodes.CompanyWithInnAlreadyExists
                                    : WorkspaceErrorCodes.CompanyWithDataAlreadyExists;
                                return result.WithError(errorCode);
                            }
                        }
                        else
                        {
                            return result.WithError(WorkspaceErrorCodes.CompanyWithNameAlreadyExists);
                        }
                    }
                    else if (request.WorkSpaceType == WorkSpaceType.Personal)
                    {
                        //для личного рабочего пространства ReviewStatus всегда проставляется NULL
                        if (request.ReviewStatus.HasValue)
                        {
                            _logger.LogError("The review status is set for personal workspace. UserId: {UserId}. " +
                                "WorkspaceName: {Name}. ReviewStatus: {Status}",
                                request.DirectorUserId, request.Name, request.ReviewStatus);
                            request.ReviewStatus = null;
                        }

                        var existsPersonalWorkspace = await _dbContext.Set<WorkSpace>()
                            .AsNoTracking()
                            .Where(x => x.WorkSpaceType == request.WorkSpaceType)
                            .Where(x => x.DirectorUserId == request.DirectorUserId)
                            .Where(x => !x.IsDeleted)
                            .AnyAsync();

                        if (existsPersonalWorkspace)
                        {
                            return result.WithError(WorkspaceErrorCodes.CanCreateOnlyOnePersonalWorkspace);
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
                newWorkSpace.ReviewStatus = request.ReviewStatus;

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
                return result.WithError(WorkspaceErrorCodes.CannotCreateOrEditWorkspace);
            }
        }

        public async Task<IDataResult<List<WorkspaceInvite>>> GetUserInvitations(int userId)
        {
            var result = new DataResult<List<WorkspaceInvite>>();

            try
            {
                var statusesNeedShow = new InviteStatus[]
                {
                    InviteStatus.Default, InviteStatus.UserDeclined
                };

                //Вытаскиваем все запросы, в которые приглашают юзера
                var statusChanges = await _dbContext.Set<WorkspaceInvite>()
                    .AsNoTracking()
                    .Include(x => x.WorkSpace)
                    .Include(x=> x.WorkSpace.DirectorUser)
                    .Include(x=> x.Inviter)
                    .Include(x=> x.User)
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
                return result.WithError(WorkspaceErrorCodes.CannotGetWpsRequests);
            }
        }

        public async Task<IDataResult<List<WorkspaceInvite>>> GetUserCreatedInvites(int userId, int workspaceId)
        {
            var result = new DataResult<List<WorkspaceInvite>>();

            try
            {
                var statusesNeedShow = new InviteStatus[]
                {
                    InviteStatus.Default, 
                    InviteStatus.UserConfirmed, 
                    InviteStatus.UserDeclined
                };
                //Вытаскиваем все запросы, в которых юзер - приглашающий
                var statusChanges = await _dbContext.Set<WorkspaceInvite>()
                    .AsNoTracking()
                    .Include(x => x.WorkSpace)
                    .Include(x=> x.User)
                    .Include(x => x.WorkSpace.DirectorUser)
                    .Include(x => x.Inviter)
                    .Where(x => x.InviterId == userId)
                    .Where(x=> x.WorkSpaceId == workspaceId)
                    .Where(x => statusesNeedShow.Contains(x.RequestStatus))
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.IsHidden)
                    .OrderBy(x => x.RequestStatus)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

                return result.WithData(statusChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user created invites.{NewLine}{Parameter}: {UserId}{NewLine2}" +
                    "{Parameter2}: {WorkspaceId}{NewLine3}",
                    Environment.NewLine, nameof(userId), userId, Environment.NewLine, nameof(workspaceId), workspaceId, Environment.NewLine);
                return result.WithError(WorkspaceErrorCodes.CannotGetWpsRequests);
            }
        }

        public async Task<IDataResult<bool>> CreateWpsInvitationRequest(WorkspaceInvite request)
        {
            var result = new DataResult<bool>();
            try
            {
                if (request.WorkSpaceId <= 0)
                {
                    return result.WithError(WorkspaceErrorCodes.WorkspaceNotSet);
                }
                else if (request.UserId <= 0)
                {
                    return result.WithError(WorkspaceErrorCodes.WpsInviteUserIdNotSet);
                }
                else if (request.InviterId <= 0)
                {
                    return result.WithError(WorkspaceErrorCodes.WpsInviterIdNotSet);
                }
                else if (request.Date == DateTime.MinValue)
                {
                    return result.WithError(WorkspaceErrorCodes.WpsInviteReqDateNotSet);
                }
                else if (request.Date > DateTime.UtcNow)
                {
                    return result.WithError(WorkspaceErrorCodes.WpsInviteReqDateFuture);
                }

                //два активных реквеста не может быть
                var activeRequestForUserExists = await _dbContext.Set<WorkspaceInvite>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x=> x.UserId == request.UserId)
                    .Where(x=> x.WorkSpaceId == request.WorkSpaceId)
                    .Where(x=> x.RequestStatus == InviteStatus.Default)
                    .AnyAsync();

                if(activeRequestForUserExists)
                    return result.WithError(WorkspaceErrorCodes.ActiveInviteAlreadyExists);

                var workspaceExists = await _dbContext.Set<WorkSpace>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.DirectorUserId == request.InviterId)
                    .Where(x => x.Id == request.WorkSpaceId)
                    .AnyAsync();

                if (!workspaceExists)
                    return result.WithError(WorkspaceErrorCodes.WpsForInviteNotExists);

                 var lastUserMemberInfo = await _dbContext.Set<WorkSpaceMember>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.UserId == request.UserId)
                    .Where(x => x.WorkSpaceId == request.WorkSpaceId)
                    .OrderBy(x=> x.Id)
                    .FirstOrDefaultAsync();

                var lastUserMemberInfoExists = lastUserMemberInfo != null;
                if (lastUserMemberInfoExists && lastUserMemberInfo.UserStatus == request.NewStatus)
                    return result.WithError(WorkspaceErrorCodes.UserAlreadyInWsp);

                var newWpsInviteRequest = new WorkspaceInvite()
                { 
                    UserId = request.UserId,
                    WorkSpaceId = request.WorkSpaceId,
                    InviterId = request.InviterId,
                    Date = request.Date,
                    PreviousStatus = lastUserMemberInfo?.UserStatus,
                    NewStatus = request.NewStatus,
                    RequestStatus = InviteStatus.Default
                };

                await _dbContext.AddAsync(newWpsInviteRequest);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating invite to WSP.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(WorkspaceErrorCodes.CannotCreateOrEditInviteWsp);
            }
        }

        public async Task<bool> IsWorkspaceMember(int userId, int workspaceId)
        {
            return await _dbContext.Set<WorkSpaceMember>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x=> !x.WorkSpace.IsDeleted)
                .Where(x => x.WorkSpaceId == workspaceId)
                .Where(x => x.UserId == userId)
                .Where(x=> x.UserStatus == UserWorkSpaceStatus.Active)
                .AnyAsync();
        }

        public async Task<bool> IsWorkspaceOwner(int userId, int workspaceId)
        {
            return await _dbContext.Set<WorkSpaceMember>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => !x.WorkSpace.IsDeleted)
                .Where(x => x.WorkSpaceId == workspaceId)
                .Where(x => x.UserId == userId)
                .Where(x => x.UserStatus == UserWorkSpaceStatus.Active)
                .Where(x=> x.TeamRole == UserTeamRole.Owner)
                .AnyAsync();
        }

        public async Task<IDataResult<List<User>>> SearchUsersForInvite(SearchUserForInvitePR searchUser)
        {
            var result = new DataResult<List<User>>();

            try
            {
                //Исключить тех, кто уже есть в вокрспейсе
                var usersAlreadyInWsp = await _dbContext.Set<WorkSpaceMember>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.WorkSpaceId == searchUser.WorkSpaceId)
                    .Select(x=> x.UserId)
                    .ToArrayAsync();

                //Вытаскиваем все запросы, в которые приглашают юзера
                var users = await _dbContext.Set<User>()
                    .AsNoTracking()
                    .Where(x=> !usersAlreadyInWsp.Contains(x.Id))
                    .Where(x => !x.IsDeleted)
                    .Where(x=> x.Email == searchUser.Search 
                    || !string.IsNullOrEmpty(x.NickName) && x.NickName == searchUser.Search)
                    .ToListAsync();

                return result.WithData(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while searching user for invite.{NewLine}{Parameter}: {SearchUser}{NewLine2}",
                    Environment.NewLine, nameof(searchUser), searchUser?.ToJson(), Environment.NewLine);
                return result.WithError(WorkspaceErrorCodes.CannotFindUserForInvite);
            }
        }

        public async Task<IDataResult<bool>> AcceptInvitationRequest(AcceptInvitePR request)
        {
            var result = new DataResult<bool>();
            try
            {
                //TODO: в контроллере проверить, что юзер - член рабочего пространства
                var errorStatuses = new InviteStatus[] { InviteStatus.All, InviteStatus.Default };
                if (request.Id <= 0)
                {
                    return result.WithError(WorkspaceErrorCodes.InviteIdNotSet);
                }
                else if (errorStatuses.Contains(request.RequestStatus))
                {
                    return result.WithError(WorkspaceErrorCodes.InvalidStatusInvite);
                }

                var activeRequest = await _dbContext.Set<WorkspaceInvite>()
                    .Where(x=> !x.IsDeleted)
                    .Where(x => x.Id == request.Id)
                    .Where(x=> x.RequestStatus == InviteStatus.Default)
                    .Where(x=> x.UserId ==  request.UserId)
                    .FirstOrDefaultAsync();

                if (activeRequest == null)
                    return result.WithError(WorkspaceErrorCodes.InviteNotExists);

                activeRequest.RequestStatus = request.RequestStatus;

               if(request.RequestStatus == InviteStatus.UserConfirmed)
               {
                    var workspaceMember = await _dbContext.Set<WorkSpaceMember>()
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted)
                        .Where(x=> !x.WorkSpace.IsDeleted)
                        .Where(x => x.WorkSpaceId == activeRequest.WorkSpaceId)
                        .Where(x=> x.UserId == activeRequest.UserId)
                        .FirstOrDefaultAsync();

                    if (workspaceMember != null)
                        workspaceMember.UserStatus = UserWorkSpaceStatus.Active;
                    else
                    {
                        //TODO: создавать WorkspaceMember в таске
                        workspaceMember = new WorkSpaceMember()
                        {
                            TeamRole = UserTeamRole.NotSet,
                            UserStatus = UserWorkSpaceStatus.Active,
                            UserId = request.UserId ?? 0,
                            WorkSpaceId = activeRequest.WorkSpaceId
                        };

                        await _dbContext.AddAsync(workspaceMember);
                    }
               }

                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while accepting invite to WSP.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(WorkspaceErrorCodes.CannotAcceptInviteWsp);
            }
        }
    }
}
