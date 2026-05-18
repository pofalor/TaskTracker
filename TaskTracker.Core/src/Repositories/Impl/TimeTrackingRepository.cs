using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Utils.src.Extensions;

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

        public Task<Dictionary<int, TimeSpan>> GetTimeSpentByIssueIdsAsync(
            IEnumerable<int> issueIds,
            CancellationToken cancellationToken = default)
        {
            var issueIdList = issueIds.ToList();
            if (issueIdList.Count == 0)
            {
                return Task.FromResult(new Dictionary<int, TimeSpan>());
            }

            return _dbSet
                .AsNoTracking()
                .Where(x => issueIdList.Contains(x.IssueId))
                .Where(x => !x.IsDeleted)
                .GroupBy(x => x.IssueId)
                .ToDictionaryAsync(
                    x => x.Key,
                    y => new TimeSpan(y.SafeSum(z => z.TimeSpent.Ticks)),
                    cancellationToken);
        }
    }
}
