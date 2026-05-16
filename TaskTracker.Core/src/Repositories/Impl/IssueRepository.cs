using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.Repositories.Impl
{
    public class IssueRepository(ApplicationDbContext context) : Repository<Issue>(context), IIssueRepository
    {
        public Task<Issue?> GetByIdNotDeletedAsync(int id, CancellationToken cancellationToken = default)
        {
            return _dbSet
                .Where(x => x.Id == id)
                .Where(x => !x.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<int> GetNextIndexAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return _dbSet
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => x.ProjectId == projectId)
                .OrderByDescending(x => x.Index)
                .Select(x => x.Index)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<bool> ExistsInProjectAsync(int issueId, int projectId, CancellationToken cancellationToken = default)
        {
            return _dbSet
                .AsNoTracking()
                .Where(x => x.Id == issueId)
                .Where(x => x.ProjectId == projectId)
                .Where(x => !x.IsDeleted)
                .AnyAsync(cancellationToken);
        }

        public async Task AddStatusHistoryAsync(IssueStatusHistory history, CancellationToken cancellationToken = default)
        {
            await _context.Set<IssueStatusHistory>().AddAsync(history, cancellationToken);
        }

        public Task<List<Issue>> GetProjectIssuesAsync(int projectId, int workspaceId, CancellationToken cancellationToken = default)
        {
            return _dbSet
                .AsNoTracking()
                .Include(x => x.Project)
                .Include(x => x.Author)
                .Include(x => x.Assignee)
                .Where(x => x.ProjectId == projectId)
                .Where(x => x.Project.WorkspaceId == workspaceId)
                .Where(x => !x.Project.IsDeleted)
                .Where(x => !x.Project.Workspace.IsDeleted)
                .Where(x => !x.IsDeleted)
                .ToListAsync(cancellationToken);
        }
    }
}
