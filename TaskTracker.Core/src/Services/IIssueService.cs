using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services
{
    public interface IIssueService
    {
        Task<IDataResult<List<IssueModel>>> GetProjectIssues(IssueFilter filter);

        Task<IDataResult<bool>> TrackTime(TimeTracking request);

        Task<IDataResult<bool>> CreateOrEdit(Issue request);

        /// <summary>
        /// Получить активный автоматический трек, если он есть. Если нет, то возвращается null.
        /// </summary>
        Task<IDataResult<TimeTracking?>> GetActiveAutoTrack(int userId, int projectId);
    }
}
