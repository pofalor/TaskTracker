using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Reflection;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Identity;
using TaskTracker.Core.src.Services;
using TaskTracker.Utils.src.Extensions;
using TaskTracker.Web.Api.Extensions;
using TaskTracker.Web.Api.Responses;

namespace TaskTracker.Web.Api.Attributes
{
    public class WorkspaceMemberAuthorizationFilter : IAsyncActionFilter
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly ILogNotificatorService _logNotificatorService;
        private readonly ApplicationDbContext _dbContext;
        private readonly WorkspaceMemberResourceType _resourceType;

        public WorkspaceMemberAuthorizationFilter(
            IWorkspaceService workspaceService,
            ILogNotificatorService logNotificatorService,
            ApplicationDbContext dbContext,
            WorkspaceMemberResourceType resourceType = WorkspaceMemberResourceType.Auto)
        {
            _workspaceService = workspaceService;
            _logNotificatorService = logNotificatorService;
            _dbContext = dbContext;
            _resourceType = resourceType;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = context.HttpContext.User.FindFirst(CustomClaimNames.UserId)?.Value.ToInt() ?? 0;
            if (userId <= 0)
            {
                context.Result = CreateErrorResult(IssueErrorCodes.AccessDenied, HttpStatusCode.Unauthorized);
                return;
            }

            var resolvedResourceType = _resourceType;
            if (resolvedResourceType == WorkspaceMemberResourceType.Auto)
            {
                if (!TryResolveAutoResourceType(context, out resolvedResourceType))
                {
                    context.Result = CreateErrorResult(SystemErrorCodes.InvalidRequest, HttpStatusCode.BadRequest);
                    return;
                }
            }

            if (!TryGetResourceId(context, resolvedResourceType, out var resourceId))
            {
                context.Result = CreateErrorResult(SystemErrorCodes.InvalidRequest, HttpStatusCode.BadRequest);
                return;
            }

            var workspaceId = await ResolveWorkspaceIdAsync(resolvedResourceType, resourceId, context.HttpContext.RequestAborted);
            if (workspaceId <= 0)
            {
                context.Result = CreateErrorResult(WorkspaceErrorCodes.UserNotFoundInWsp, HttpStatusCode.NotFound);
                return;
            }

            var isMember = await _workspaceService.IsWorkspaceMember(userId, workspaceId);
            if (!isMember)
            {
                await _logNotificatorService.SendTelegramAdminAsync(
                    $"User attempted access to a resource in a workspace they do not belong to.{Environment.NewLine}" +
                    $"Workspace ID: {workspaceId}{Environment.NewLine}" +
                    $"User ID: {userId}.");

                context.Result = CreateErrorResult(IssueErrorCodes.UserNotMemberWsp, HttpStatusCode.UnprocessableEntity);
                return;
            }

            await next();
        }

        private static bool TryResolveAutoResourceType(ActionExecutingContext context, out WorkspaceMemberResourceType resourceType)
        {
            resourceType = WorkspaceMemberResourceType.Project;

            if (TryGetPositiveIntFromArguments(context, "Id", out _) || TryGetPositiveIntFromArguments(context, "IssueId", out _))
            {
                resourceType = WorkspaceMemberResourceType.Issue;
                return true;
            }

            if (TryGetPositiveIntFromArguments(context, "ProjectId", out _))
            {
                resourceType = WorkspaceMemberResourceType.Project;
                return true;
            }

            if (TryGetPositiveIntFromArguments(context, "WorkspaceId", out _))
            {
                resourceType = WorkspaceMemberResourceType.Workspace;
                return true;
            }

            return false;
        }

        private static bool TryGetResourceId(ActionExecutingContext context, WorkspaceMemberResourceType resourceType, out int resourceId)
        {
            resourceId = 0;

            return resourceType switch
            {
                WorkspaceMemberResourceType.Issue =>
                    TryGetPositiveIntFromArguments(context, "Id", out resourceId)
                    || TryGetPositiveIntFromArguments(context, "IssueId", out resourceId),
                WorkspaceMemberResourceType.Project => TryGetPositiveIntFromArguments(context, "ProjectId", out resourceId),
                WorkspaceMemberResourceType.Workspace => TryGetPositiveIntFromArguments(context, "WorkspaceId", out resourceId),
                _ => false,
            };
        }

        private static bool TryGetPositiveIntFromArguments(ActionExecutingContext context, string propertyName, out int value)
        {
            value = 0;

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null)
                {
                    continue;
                }

                var property = argument.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                {
                    continue;
                }

                var propertyValue = property.GetValue(argument);
                if (propertyValue is int intValue && intValue > 0)
                {
                    value = intValue;
                    return true;
                }
            }

            return false;
        }

        private async Task<int> ResolveWorkspaceIdAsync(
            WorkspaceMemberResourceType resourceType,
            int resourceId,
            CancellationToken cancellationToken)
        {
            return resourceType switch
            {
                WorkspaceMemberResourceType.Issue => await _dbContext.Set<Issue>()
                    .AsNoTracking()
                    .Where(i => i.Id == resourceId)
                    .Where(i => !i.IsDeleted)
                    .Join(
                        _dbContext.Set<Project>().Where(p => !p.IsDeleted),
                        i => i.ProjectId,
                        p => p.Id,
                        (i, p) => p.WorkspaceId)
                    .FirstOrDefaultAsync(cancellationToken),
                WorkspaceMemberResourceType.Project => await _dbContext.Set<Project>()
                    .AsNoTracking()
                    .Where(p => p.Id == resourceId)
                    .Where(p => !p.IsDeleted)
                    .Where(p => !p.Workspace.IsDeleted)
                    .Select(p => p.WorkspaceId)
                    .FirstOrDefaultAsync(cancellationToken),
                WorkspaceMemberResourceType.Workspace => resourceId,
                _ => 0,
            };
        }

        private static ObjectResult CreateErrorResult(Enum errorCode, HttpStatusCode statusCode)
        {
            var response = new DataResponse<bool>().WithError(errorCode);
            return new ObjectResult(response) { StatusCode = (int)statusCode };
        }
    }
}
