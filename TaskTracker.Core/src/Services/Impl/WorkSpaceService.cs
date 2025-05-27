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

        public async Task<IDataResult<List<WorkspaceMember>>> GetMyWorkspaces(int userId)
        {
            var result = new DataResult<List<WorkspaceMember>>();

            try
            {
                //Вытаскиваем все организации, где юзер - член рабочего пространства
                //(здесь будут содержаться также организации, которые создал юзер,
                //т.к. на юзера создаётся WorkspaceMember UserTeamRole - Owner)
                var workspaces = await _dbContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .Include(x=> x.Workspace)
                    .Where(x=> x.UserId == userId)
                    .Where(x=> x.UserStatus != UserWorkspaceStatus.Deleted)
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

        public async Task<IDataResult<bool>> CreateOrEdit(Workspace request)
        {
            var result = new DataResult<bool>();
            try
            {
                var newWorkspace = new Workspace();

                var existingWorkspace = await _dbContext.Set<Workspace>()
                        .Where(x => request.Id == x.Id)
                        .Where(x => !x.IsDeleted)
                        .FirstOrDefaultAsync();

                //если не пусто, значит изменяем, иначе добавляем новое рабочее пространство
                if (existingWorkspace != null) 
                {
                    newWorkspace = existingWorkspace;
                }
                else
                {
                    if (request.WorkspaceType == WorkspaceType.Company)
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
                            var workspaceWithSameName = await _dbContext.Set<Workspace>()
                            .AsNoTracking()
                            .Where(x => x.Name == request.Name)
                            .Where(x => x.DirectorUserId == request.DirectorUserId)
                            .Where(x => x.WorkspaceType == request.WorkspaceType)
                            .Where(x => !x.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (workspaceWithSameName == null)
                        {
                            //Не должно быть рабочих пространств с одинаковыми инн, не должно быть одинаковых комбинаций:
                            //даты регистрации, юр. адреса, страны.
                            var existingWorkspaceByCompanyFields = await _dbContext.Set<Workspace>()
                                .AsNoTracking()
                                .Where(x => x.INN == request.INN
                                || (x.Country == request.Country && x.Address == request.Address && x.RegistrationDate == request.RegistrationDate))
                                .Where(x => x.WorkspaceType == request.WorkspaceType)
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
                    else if (request.WorkspaceType == WorkspaceType.Personal)
                    {
                        //для личного рабочего пространства ReviewStatus всегда проставляется NULL
                        if (request.ReviewStatus.HasValue)
                        {
                            _logger.LogError("The review status is set for personal workspace. UserId: {UserId}. " +
                                "WorkspaceName: {Name}. ReviewStatus: {Status}",
                                request.DirectorUserId, request.Name, request.ReviewStatus);
                            request.ReviewStatus = null;
                        }

                        var existsPersonalWorkspace = await _dbContext.Set<Workspace>()
                            .AsNoTracking()
                            .Where(x => x.WorkspaceType == request.WorkspaceType)
                            .Where(x => x.DirectorUserId == request.DirectorUserId)
                            .Where(x => !x.IsDeleted)
                            .AnyAsync();

                        if (existsPersonalWorkspace)
                        {
                            return result.WithError(WorkspaceErrorCodes.CanCreateOnlyOnePersonalWorkspace);
                        }
                    }
                    newWorkspace.WorkspaceType = request.WorkspaceType;
                    newWorkspace.DirectorUserId = request.DirectorUserId;
                }
                //TODO: сделать через автомаппер
                newWorkspace.Name = request.Name;
                newWorkspace.Country = request.Country;
                newWorkspace.RegistrationDate = request.RegistrationDate;
                newWorkspace.Address = request.Address;
                newWorkspace.INN = request.INN;
                newWorkspace.ReviewStatus = request.ReviewStatus;

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    if(existingWorkspace == null)
                        await _dbContext.AddAsync(newWorkspace);
                    await _dbContext.SaveChangesAsync();

                    //создаём сотрудника(владельца) для новой компании
                    if (existingWorkspace == null)
                    {
                        var newWorkspaceMember = new WorkspaceMember()
                        {
                            UserId = newWorkspace.DirectorUserId,
                            TeamRole = UserTeamRole.Owner,
                            UserStatus = UserWorkspaceStatus.Active,
                            WorkspaceId = newWorkspace.Id,
                        };

                        await _dbContext.AddAsync(newWorkspaceMember);
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
                    .Include(x => x.Workspace)
                    .Include(x=> x.Workspace.DirectorUser)
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
                    .Include(x => x.Workspace)
                    .Include(x=> x.User)
                    .Include(x => x.Workspace.DirectorUser)
                    .Include(x => x.Inviter)
                    .Where(x => x.InviterId == userId)
                    .Where(x=> x.WorkspaceId == workspaceId)
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
                if (request.WorkspaceId <= 0)
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
                    .Where(x=> x.WorkspaceId == request.WorkspaceId)
                    .Where(x=> x.RequestStatus == InviteStatus.Default)
                    .AnyAsync();

                if(activeRequestForUserExists)
                    return result.WithError(WorkspaceErrorCodes.ActiveInviteAlreadyExists);

                var workspaceExists = await _dbContext.Set<Workspace>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.DirectorUserId == request.InviterId)
                    .Where(x => x.Id == request.WorkspaceId)
                    .AnyAsync();

                if (!workspaceExists)
                    return result.WithError(WorkspaceErrorCodes.WpsForInviteNotExists);

                 var lastUserMemberInfo = await _dbContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.UserId == request.UserId)
                    .Where(x => x.WorkspaceId == request.WorkspaceId)
                    .OrderBy(x=> x.Id)
                    .FirstOrDefaultAsync();

                var lastUserMemberInfoExists = lastUserMemberInfo != null;
                if (lastUserMemberInfoExists && lastUserMemberInfo.UserStatus == request.NewStatus)
                    return result.WithError(WorkspaceErrorCodes.UserAlreadyInWsp);

                var newWpsInviteRequest = new WorkspaceInvite()
                { 
                    UserId = request.UserId,
                    WorkspaceId = request.WorkspaceId,
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
            return await _dbContext.Set<WorkspaceMember>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x=> !x.Workspace.IsDeleted)
                .Where(x => x.WorkspaceId == workspaceId)
                .Where(x => x.UserId == userId)
                .Where(x=> x.UserStatus == UserWorkspaceStatus.Active)
                .AnyAsync();
        }

        public async Task<bool> IsWorkspaceOwner(int userId, int workspaceId)
        {
            return await _dbContext.Set<WorkspaceMember>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => !x.Workspace.IsDeleted)
                .Where(x => x.WorkspaceId == workspaceId)
                .Where(x => x.UserId == userId)
                .Where(x => x.UserStatus == UserWorkspaceStatus.Active)
                .Where(x=> x.TeamRole == UserTeamRole.Owner)
                .AnyAsync();
        }

        public async Task<IDataResult<List<User>>> SearchUsersForInvite(SearchUserForInvitePR searchUser)
        {
            var result = new DataResult<List<User>>();

            try
            {
                //Исключить тех, кто уже есть в вокрспейсе
                var usersAlreadyInWsp = await _dbContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.WorkspaceId == searchUser.WorkspaceId)
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
