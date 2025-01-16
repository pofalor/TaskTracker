using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class AcceptInvitePR
    {
        public int Id { get; set; }

        public UserStatusChangeType RequestStatus { get; set; }

        /// <summary>
        /// Тот, кто принимает запрос
        /// </summary>
        public int? UserId { get; set; }
    }
}
