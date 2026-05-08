using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Services;

public class WorkspaceMemberFilterAttribute : IAsyncActionFilter
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogNotificatorService _logNotificatorService;
    private readonly ApplicationDbContext _dbContext;

    public WorkspaceMemberFilterAttribute(
        IWorkspaceService workspaceService,
        ILogNotificatorService logNotificatorService,
        ApplicationDbContext dbContext)
    {
        _workspaceService = workspaceService;
        _logNotificatorService = logNotificatorService;
        _dbContext = dbContext;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Получить UserId из токена
        var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Найти в параметрах метода объект, содержащий ID сущности (IssueId, ProjectId и т.д.)
        // На основе этого ID будет получен WorkspaceId через связи в БД
        object? resourceKey = null;
        string resourceType = null;

        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg == null) continue;

            var type = arg.GetType();

            // Проверяем наличие свойств типа ID в DTO
            if (type.GetProperty("IssueId")?.GetValue(arg) is int issueId && issueId > 0)
            {
                resourceKey = issueId;
                resourceType = "Issue";
                break;
            }
            if (type.GetProperty("ProjectId")?.GetValue(arg) is int projectId && projectId > 0)
            {
                resourceKey = projectId;
                resourceType = "Project";
                break;
            }
            if (type.GetProperty("WorkspaceId")?.GetValue(arg) is int workspaceIdInt && workspaceIdInt > 0)
            {
                resourceKey = workspaceIdInt;
                resourceType = "Workspace";
                break;
            }
            // Добавьте другие типы сущностей по необходимости
            // Например: if (type.GetProperty("TaskId")?.GetValue(arg) is int taskId && taskId > 0) ...
        }

        if (resourceKey == null)
        {
            context.Result = new BadRequestObjectResult(new { Error = "Resource ID (e.g. IssueId, ProjectId, WorkspaceId) not found in request." });
            return;
        }

        // Определить WorkspaceId на основе типа сущности
        int workspaceId = 0;
        using (var dbContext = _dbContext.CreateDbContext(context.HttpContext.RequestAborted))
        {
            switch (resourceType)
            {
                case "Issue":
                    workspaceId = await dbContext.Set<Issue>()
                        .Where(i => i.Id == (int)resourceKey)
                        .Where(i => !i.IsDeleted)
                        .Join(dbContext.Set<Project>(), i => i.ProjectId, p => p.Id, (i, p) => p)
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.WorkspaceId)
                        .FirstOrDefaultAsync();
                    break;
                case "Project":
                    workspaceId = await dbContext.Set<Project>()
                        .Where(p => p.Id == (int)resourceKey)
                        .Where(p => !p.IsDeleted)
                        .Select(p => p.WorkspaceId)
                        .FirstOrDefaultAsync();
                    break;
                case "Workspace":
                    // Если уже передан WorkspaceId, используем его напрямую
                    workspaceId = (int)resourceKey;
                    break;
                default:
                    context.Result = new BadRequestObjectResult(new { Error = "Unknown resource type." });
                    return;
            }
        }

        if (workspaceId == 0)
        {
            context.Result = new NotFoundObjectResult(new { Error = "Resource not found or invalid." });
            return;
        }

        // Вызов вашего метода IsWorkspaceMember
        var isMember = await _workspaceService.IsWorkspaceMember(userId, workspaceId);

        if (!isMember)
        {
            await _logNotificatorService.SendTelegramAdminAsync(
                $"User attempted access to a resource in a workspace they do not belong to.{Environment.NewLine}" +
                $"Workspace ID: {workspaceId}{Environment.NewLine}" +
                $"User ID: {userId}.");

            context.Result = new ForbidResult(); // 403
            return;
        }

        // Если проверка пройдена, продолжаем выполнение
        await next();
    }
}