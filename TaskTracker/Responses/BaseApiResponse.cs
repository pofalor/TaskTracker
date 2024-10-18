using TaskTracker.Core.src.DataResult;

namespace TaskTracker.Web.Api.Responses
{
    public class BaseApiResponse
    {
        public BaseApiResponse()
        {
            Errors = new List<IDataError>();
        }

        public IList<IDataError> Errors { get; private set; }
    }
}