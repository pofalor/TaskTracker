using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Services
{
    public interface IIssueEstimatePredictionService
    {
        Task<IDataResult<IssueEstimatePredictionModel>> PredictEstimateAsync(IssueEstimatePredictionPR request);
    }
}
