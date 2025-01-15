namespace TaskTracker.Core.src.Models.PostRequests
{
    public class SearchUserForInvitePR
    {
        public int WorkSpaceId { get; set; }

        public int? InviterId { get; set; }

        public string Search { get; set; } = string.Empty;
    }
}