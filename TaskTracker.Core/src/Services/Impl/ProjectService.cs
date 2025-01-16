using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.ErrorCodes;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class ProjectService : BaseService<Project, BaseFilter>, IProjectService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ProjectService> _logger;
        private readonly IWorkSpaceService _workSpaceService;

        public ProjectService(ApplicationDbContext dbContext, ILogger<ProjectService> logger, IWorkSpaceService workSpaceService) :
            base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _workSpaceService = workSpaceService;
        }

        public async Task<IDataResult<List<Project>>> GetWorkspaceProjects(int workspaceId)
        {
            var result = new DataResult<List<Project>>();

            try
            {
                //Вытаскиваем все проекты организации
                var projects = await _dbContext.Set<Project>()
                    .AsNoTracking()
                    .Where(x=> x.WorkSpaceId == workspaceId)
                    .Include(x => x.WorkSpace)
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                return result.WithData(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting workspace projects.{NewLine}{Parameter}: {WorkspaceId}{NewLine2}",
                    Environment.NewLine, nameof(workspaceId), workspaceId, Environment.NewLine);
                return result.WithError(ProjectErrorCodes.CannotGetProjects);
            }
        }

        public override async Task<IDataResult<bool>> CreateOrEdit(Project request)
        {
            //TODO: добавить проверку, что автор текущий юзер
            var result = new DataResult<bool>();
            try
            {
                if (request.WorkSpaceId <= 0)
                {
                    return result.WithError(ProjectErrorCodes.WorkspaceNotSet);
                }
                else if (request.ProjectMgrId <= 0)
                {
                    return result.WithError(ProjectErrorCodes.ProjectMgrNotSet);
                }
                else if (request.AuthorId <= 0)
                {
                    return result.WithError(ProjectErrorCodes.AuthorNotSet);
                }
                else if (request.EndDate.HasValue && request.EndDate.Value == DateTime.MinValue)
                {
                    return result.WithError(ProjectErrorCodes.EndDateNotSet);
                }
                else if (request.StartDate > DateTime.UtcNow)
                {
                    return result.WithError(ProjectErrorCodes.StartDateInFuture);
                }
                else if (request.Name.IsEmpty())
                {
                    return result.WithError(ProjectErrorCodes.ProjectEmptyName);
                }
                else if (request.Code.IsEmpty())
                {
                    return result.WithError(ProjectErrorCodes.ProjectEmptyName);
                }

                //два активных реквеста не может быть
                var projectWithNameOrCodeExists = await _dbContext.Set<Project>()
                    .AsNoTracking()
                    .Where(x=> x.WorkSpaceId ==  request.WorkSpaceId)
                    .Where(x => !x.IsDeleted)
                    .Where(x => x.Name == request.Name 
                    || x.Code == request.Code)
                    .AnyAsync();

                if (projectWithNameOrCodeExists)
                    return result.WithError(ProjectErrorCodes.ProjectWithNameOrCodeExists);

                var isProjectMgrInWsp = await _workSpaceService.IsWorkspaceMember(request.ProjectMgrId, request.WorkSpaceId);

                if (!isProjectMgrInWsp)
                {
                    return result.WithError(ProjectErrorCodes.ProjectMgrNotWspMember);
                }

                var newProject = new Project();
                var existingProject = await _dbContext.Set<Project>()
                       .Where(x => request.Id == x.Id)
                       .Where(x => !x.IsDeleted)
                       .FirstOrDefaultAsync();

                //если не пусто, значит изменяем, иначе добавляем новое рабочее пространство
                if (existingProject != null)
                {
                    newProject = existingProject;
                }
                else
                {
                    newProject.AuthorId = request.AuthorId;
                }

                newProject.Name = request.Name;
                newProject.Description = request.Description;
                newProject.Code = request.Code;
                newProject.StartDate = request.StartDate;
                newProject.EndDate = request.EndDate;
                newProject.ProjectMgrId = request.ProjectMgrId;
                newProject.WorkSpaceId = request.WorkSpaceId;

                if(existingProject == null)
                    await _dbContext.AddAsync(newProject);
                await _dbContext.SaveChangesAsync();

                return result.WithData(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating project.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(ProjectErrorCodes.CannotCreateProject);
            }
        }
    }
}
