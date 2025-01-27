using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Constants
{
    public static class IssueConstants
    {
        public static readonly IssueType[] ValidIssueTypes =
        [
            IssueType.Epic,
            IssueType.Story,
            IssueType.Bug,
            IssueType.Task,
        ];

        public static readonly IssueStatus[] ValidIssueStatuses =
        [
            IssueStatus.Backlog,
            IssueStatus.SelectedForDevelopment,
            IssueStatus.InProgress,
            IssueStatus.PullRequest,
            IssueStatus.ToDeploy,
            IssueStatus.Test,
            IssueStatus.Declined,
            IssueStatus.Done,
            IssueStatus.Deferred
        ];

        public static readonly IssuePriority[] ValidIssuePriorities =
        [
            IssuePriority.Lowest,
            IssuePriority.Low,
            IssuePriority.Medium,
            IssuePriority.High,
            IssuePriority.Highest
        ];
    }
}
