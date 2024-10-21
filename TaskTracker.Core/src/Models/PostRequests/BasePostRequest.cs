using System.Web;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class BasePostRequest
    {
        public string IP { get; set; } = string.Empty;

        public string Localization { get; set; } = string.Empty;
    }
}
