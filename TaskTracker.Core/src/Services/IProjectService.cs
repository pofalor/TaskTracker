using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;

namespace TaskTracker.Core.src.Services
{
    public interface IProjectService : IBaseService<Project, BaseFilter>
    {
        Task<IDataResult<List<Project>>> GetWorkspaceProjects(int workspaceId);
    }
}
