namespace TaskTracker.Core.src.Models.ResponseModels
{
    public class IssueEstimatePredictionModel
    {
        public int EstimateSeconds { get; set; }

        public string Estimate { get; set; } = string.Empty;

        public bool UsedMlModel { get; set; }

        public int TrainingSamples { get; set; }

        public double Confidence { get; set; }

        public List<IssueEstimatePredictionFactorModel> Factors { get; set; } = [];
    }
}
