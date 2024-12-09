using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;

namespace TaskTracker.Core.src.Services
{
    public interface IWorkSpaceService : IBaseService<WorkSpace, WorkSpaceFilter>
    {
        Task<IDataResult<WorkSpace[]>> GetMyWorkspaces(int userId);
    }
}
