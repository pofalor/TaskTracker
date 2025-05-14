namespace TaskTracker.Core.src.Models.Filters
{
    public class IssueFilter : BaseFilter
    {
        public int WorkspaceId { get; set; }

        public int ProjectId { get; set; }
    }
}
