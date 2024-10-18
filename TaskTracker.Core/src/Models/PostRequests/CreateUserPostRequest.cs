namespace TaskTracker.Core.src.Models.PostRequests
{
    public class CreateUserPostRequest : BasePostRequest
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = null!;

        public int? Country { get; set; }

        public string Password { get; set; } = null!;
    }
}
