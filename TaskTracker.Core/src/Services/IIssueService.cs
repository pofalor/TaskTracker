using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services
{
    public interface IIssueService : IBaseService<Issue, IssueFilter>
    {
        Task<IDataResult<List<IssueModel>>> GetProjectIssues(IssueFilter filter);

        Task<IDataResult<bool>> TrackTime(TimeTracking request);
    }
}
