using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Repositories.Impl
{
    public class TimeTrackingRepository(ApplicationDbContext context) : Repository<TimeTracking>(context), ITimeTrackingRepository
    {
        private static readonly AutoTrackTimeStatus[] ActiveAutoTrackStatuses =
        {
            AutoTrackTimeStatus.Active,
            AutoTrackTimeStatus.Stopped,
        };


        public Task<bool> HasActiveAutoTrackOnIssueAsync(int issueId, CancellationToken cancellationToken = default)
        {
            return _dbSet
                .AsNoTracking()
                .Where(x => x.IssueId == issueId)
                .Where(x => !x.IsDeleted)
                .Where(x => x.AutoTrackStatus.HasValue)
                .Where(x => ActiveAutoTrackStatuses.Contains(x.AutoTrackStatus!.Value))
                .AnyAsync(cancellationToken);
        }
    }
}
