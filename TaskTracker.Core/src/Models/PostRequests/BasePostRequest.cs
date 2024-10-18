using System.Web;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class BasePostRequest
    {
        public string IP { get; set; } = null!;

        public string Localization { get; set; } = string.Empty;
    }
}
