using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class CreateWspInvitePostRequest : BasePostRequest
    {

        public int WorkSpaceId { get; set; }

        public int UserId { get; set; }

        public int InviterId { get; set; }

        /// <summary>
        /// Дата, когда был создан запрос в UTC
        /// </summary>
        public string Date { get; set; } = DateTime.UtcNow.ToString(DateFormatConstants.FrontInputFormat);
        public UserWorkSpaceStatus PreviousStatus { get; set; }

        public UserWorkSpaceStatus NewStatus { get; set; }
    }
}
