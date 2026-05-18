using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class IssueEstimatePredictionPR : BasePostRequest
    {
        public int? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IssueType Type { get; set; }

        public IssueStatus Status { get; set; }

        public IssuePriority Priority { get; set; }

        public int? ParentId { get; set; }

        public int? AssigneeId { get; set; }

        public int ProjectId { get; set; }
    }
}
