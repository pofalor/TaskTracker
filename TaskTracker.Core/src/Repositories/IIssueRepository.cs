using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.Repositories
{
    public interface IIssueRepository : IRepository<Issue>
    {
        Task<Issue?> GetByIdNotDeletedAsync(int id, CancellationToken cancellationToken = default);

        Task<int> GetNextIndexAsync(int projectId, CancellationToken cancellationToken = default);

        Task<bool> ExistsInProjectAsync(int issueId, int projectId, CancellationToken cancellationToken = default);

        Task AddStatusHistoryAsync(IssueStatusHistory history, CancellationToken cancellationToken = default);

        Task<List<Issue>> GetProjectIssuesAsync(int projectId, int workspaceId, CancellationToken cancellationToken = default);
    }
}
