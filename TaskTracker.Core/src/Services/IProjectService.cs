using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;

namespace TaskTracker.Core.src.Services
{
    public interface IProjectService
    {
        Task<IDataResult<List<Project>>> GetWorkspaceProjects(int workspaceId);

        Task<IDataResult<List<User>>> GetProjectMgrCandidates(int workspaceId);

        Task<IDataResult<bool>> CreateOrEdit(Project request);
    }
}
