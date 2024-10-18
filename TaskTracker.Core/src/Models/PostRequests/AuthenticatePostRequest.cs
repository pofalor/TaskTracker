namespace TaskTracker.Core.src.Models.PostRequests
{
    public class AuthenticatePostRequest : BasePostRequest
    {
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
