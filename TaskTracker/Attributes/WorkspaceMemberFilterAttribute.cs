using Microsoft.AspNetCore.Mvc;

namespace TaskTracker.Web.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class WorkspaceMemberFilterAttribute : TypeFilterAttribute
    {
        public WorkspaceMemberFilterAttribute(WorkspaceMemberResourceType resourceType = WorkspaceMemberResourceType.Auto)
            : base(typeof(WorkspaceMemberAuthorizationFilter))
        {
            Arguments = new object[] { resourceType };
        }
    }
}
