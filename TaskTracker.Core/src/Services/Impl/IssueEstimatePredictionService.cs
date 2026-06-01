using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.DataResult;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.Services.Impl
{
    public class IssueEstimatePredictionService : IIssueEstimatePredictionService
    {
        private const int MinMlTrainingSamples = 10;
        private const int MinEstimateSeconds = 60;
        private const int MaxEstimateSeconds = 30 * 24 * 60 * 60;
        private const int ProductivityRecentWindowDays = 90;
        private const double MinEstimateAccuracyRatio = 0.25;
        private const double MaxEstimateAccuracyRatio = 4;

        private static readonly HashSet<IssueStatus> TerminalIssueStatuses =
        [
            IssueStatus.Done,
            IssueStatus.Declined,
            IssueStatus.Deferred,
        ];

        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<IssueEstimatePredictionService> _logger;

        public IssueEstimatePredictionService(
            ApplicationDbContext dbContext,
            ILogger<IssueEstimatePredictionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IDataResult<IssueEstimatePredictionModel>> PredictEstimateAsync(IssueEstimatePredictionPR request)
        {
            var result = new DataResult<IssueEstimatePredictionModel>();

            try
            {
                var validationError = ValidateRequest(request);
                if (validationError.HasValue)
                {
                    return result.WithError(validationError.Value);
                }

                var project = await _dbContext.Set<Project>()
                    .AsNoTracking()
                    .Where(x => x.Id == request.ProjectId)
                    .Where(x => !x.IsDeleted)
                    .Where(x => !x.Workspace.IsDeleted)
                    .Select(x => new ProjectPredictionScope
                    {
                        ProjectId = x.Id,
                        WorkspaceId = x.WorkspaceId,
                    })
                    .FirstOrDefaultAsync();

                if (project == null)
                {
                    return result.WithError(IssueErrorCodes.ProjectNotSet);
                }

                var issueRows = await LoadIssueRowsAsync(project.WorkspaceId);
                var trackedTimeByIssue = await LoadTrackedTimeByIssueAsync(project.WorkspaceId);
                var completionDatesByIssue = await LoadCompletionDatesByIssueAsync(project.WorkspaceId);
                var samples = BuildSamples(issueRows, trackedTimeByIssue, request.Id, completionDatesByIssue);
                var metrics = BuildMetrics(samples, issueRows, request.Id);
                var heuristic = PredictByHeuristic(request, metrics);

                var predictedSeconds = heuristic.Seconds;
                var usedMlModel = false;

                if (CanUseMl(samples))
                {
                    var mlPredictionSeconds = TryPredictWithMl(request, project, samples, metrics);
                    if (mlPredictionSeconds.HasValue)
                    {
                        predictedSeconds = BlendMlWithHeuristic(mlPredictionSeconds.Value, heuristic.Seconds, samples.Count);
                        usedMlModel = true;
                    }
                }

                var estimateSeconds = RoundEstimateSeconds(ClampSeconds(predictedSeconds));
                var model = new IssueEstimatePredictionModel
                {
                    EstimateSeconds = estimateSeconds,
                    Estimate = FormatTimeTrackString(TimeSpan.FromSeconds(estimateSeconds)),
                    UsedMlModel = usedMlModel,
                    TrainingSamples = samples.Count,
                    Confidence = CalculateConfidence(samples.Count, request.AssigneeId, request.ProjectId, metrics, usedMlModel),
                    Factors = BuildFactors(request, metrics, heuristic, usedMlModel, samples.Count),
                };

                return result.WithData(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while predicting issue estimate.{NewLine}{Parameter}: {Request}{NewLine2}",
                    Environment.NewLine, nameof(request), request?.ToJson(), Environment.NewLine);
                return result.WithError(IssueErrorCodes.CannotGetIssues);
            }
        }

        private static IssueErrorCodes? ValidateRequest(IssueEstimatePredictionPR request)
        {
            if (request.ProjectId <= 0)
            {
                return IssueErrorCodes.ProjectNotSet;
            }

            if (!IssueConstants.ValidIssueTypes.Contains(request.Type))
            {
                return IssueErrorCodes.IssueTypeInvalid;
            }

            if (!IssueConstants.ValidIssueStatuses.Contains(request.Status))
            {
                return IssueErrorCodes.IssueStatusInvalid;
            }

            if (!IssueConstants.ValidIssuePriorities.Contains(request.Priority))
            {
                return IssueErrorCodes.IssuePriorityInvalid;
            }

            if (request.AssigneeId.HasValue && request.AssigneeId.Value <= 0)
            {
                return IssueErrorCodes.IssueAssigneeInvalid;
            }

            return null;
        }

        private Task<List<IssueHistoryRow>> LoadIssueRowsAsync(int workspaceId)
        {
            return _dbContext.Set<Issue>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => !x.Project.IsDeleted)
                .Where(x => !x.Project.Workspace.IsDeleted)
                .Where(x => x.Project.WorkspaceId == workspaceId)
                .Select(x => new IssueHistoryRow
                {
                    Id = x.Id,
                    ObjectCreateDate = x.ObjectCreateDate,
                    ObjectEditDate = x.ObjectEditDate,
                    NameLength = x.Name.Length,
                    DescriptionLength = x.Description.Length,
                    Type = x.Type,
                    Status = x.Status,
                    Priority = x.Priority,
                    Estimate = x.Estimate,
                    ParentId = x.ParentId,
                    AssigneeId = x.AssigneeId,
                    ProjectId = x.ProjectId,
                })
                .ToListAsync();
        }

        private async Task<Dictionary<int, TrackedTimeSummary>> LoadTrackedTimeByIssueAsync(int workspaceId)
        {
            var timeRows = await _dbContext.Set<TimeTracking>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => x.TimeSpent > TimeSpan.Zero)
                .Where(x => !x.Issue.IsDeleted)
                .Where(x => !x.Issue.Project.IsDeleted)
                .Where(x => !x.Issue.Project.Workspace.IsDeleted)
                .Where(x => x.Issue.Project.WorkspaceId == workspaceId)
                .Select(x => new TrackedTimeRow
                {
                    IssueId = x.IssueId,
                    TimeSpent = x.TimeSpent,
                    DateBegin = x.DateBegin,
                })
                .ToListAsync();

            return timeRows
                .GroupBy(x => x.IssueId)
                .ToDictionary(
                    x => x.Key,
                    x => new TrackedTimeSummary
                    {
                        Seconds = x.Sum(y => y.TimeSpent.TotalSeconds),
                        LastTrackedAt = x.Max(y => y.DateBegin),
                    });
        }

        private async Task<Dictionary<int, DateTime>> LoadCompletionDatesByIssueAsync(int workspaceId)
        {
            var completionRows = await _dbContext.Set<IssueStatusHistory>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => x.NewStatus == IssueStatus.Done)
                .Where(x => !x.Issue.IsDeleted)
                .Where(x => !x.Issue.Project.IsDeleted)
                .Where(x => !x.Issue.Project.Workspace.IsDeleted)
                .Where(x => x.Issue.Project.WorkspaceId == workspaceId)
                .Select(x => new IssueCompletionRow
                {
                    IssueId = x.IssueId,
                    ChangedAt = x.ChangedAt,
                })
                .ToListAsync();

            return completionRows
                .GroupBy(x => x.IssueId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Min(y => y.ChangedAt));
        }

        private static List<IssueSample> BuildSamples(
            IEnumerable<IssueHistoryRow> issueRows,
            IReadOnlyDictionary<int, TrackedTimeSummary> trackedTimeByIssue,
            int? currentIssueId,
            IReadOnlyDictionary<int, DateTime> completionDatesByIssue)
        {
            return issueRows
                .Where(x => !currentIssueId.HasValue || x.Id != currentIssueId.Value)
                .Select(x => trackedTimeByIssue.TryGetValue(x.Id, out var trackedTime)
                    ? new IssueSample(x, trackedTime, GetCompletedAt(x, completionDatesByIssue))
                    : null)
                .Where(x => x is { ActualSeconds: > 0 })
                .Select(x => x!)
                .ToList();
        }

        private static DateTime? GetCompletedAt(
            IssueHistoryRow issue,
            IReadOnlyDictionary<int, DateTime> completionDatesByIssue)
        {
            if (completionDatesByIssue.TryGetValue(issue.Id, out var completedAt))
            {
                return completedAt;
            }

            return issue.Status == IssueStatus.Done
                ? issue.ObjectEditDate
                : null;
        }

        private static PredictionMetrics BuildMetrics(
            List<IssueSample> samples,
            IReadOnlyCollection<IssueHistoryRow> issueRows,
            int? currentIssueId)
        {
            var referenceDate = GetReferenceDate(samples);
            var recentThreshold = referenceDate.AddDays(-ProductivityRecentWindowDays);
            var recentSamples = samples
                .Where(x => x.LastActivityAt >= recentThreshold)
                .ToList();
            var completedSamples = samples
                .Where(x => x.CompletedAt.HasValue)
                .ToList();
            var recentCompletedSamples = completedSamples
                .Where(x => x.CompletedAt >= recentThreshold)
                .ToList();
            var assigneeThroughputPerWeek = GroupThroughputPerWeek(
                recentCompletedSamples.Where(x => x.AssigneeId.HasValue),
                x => x.AssigneeId!.Value,
                ProductivityRecentWindowDays);

            return new PredictionMetrics
            {
                WorkspaceAverageSeconds = TrimmedAverage(samples.Select(x => x.ActualSeconds)),
                ProjectAverageSeconds = GroupAverage(samples, x => x.ProjectId),
                ProjectSampleCounts = GroupCount(samples, x => x.ProjectId),
                AssigneeAverageSeconds = GroupAverage(
                    samples.Where(x => x.AssigneeId.HasValue),
                    x => x.AssigneeId!.Value),
                AssigneeSampleCounts = GroupCount(
                    samples.Where(x => x.AssigneeId.HasValue),
                    x => x.AssigneeId!.Value),
                AssigneeRecentAverageSeconds = GroupAverage(
                    recentSamples.Where(x => x.AssigneeId.HasValue),
                    x => x.AssigneeId!.Value),
                AssigneeEstimateAccuracyRatio = GroupAverage(
                    samples.Where(x => x.AssigneeId.HasValue && x.EstimateSeconds is > 0),
                    x => x.AssigneeId!.Value,
                    x => ClampEstimateAccuracyRatio(x.ActualSeconds / x.EstimateSeconds!.Value)),
                WorkspaceEstimateAccuracyRatio = TrimmedAverage(
                    samples
                        .Where(x => x.EstimateSeconds is > 0)
                        .Select(x => ClampEstimateAccuracyRatio(x.ActualSeconds / x.EstimateSeconds!.Value))),
                AssigneeThroughputPerWeek = assigneeThroughputPerWeek,
                WorkspaceThroughputPerWeek = assigneeThroughputPerWeek.Count > 0
                    ? assigneeThroughputPerWeek.Values.Average()
                    : 0,
                AssigneeOpenIssueCounts = GroupOpenIssuesByAssignee(issueRows, currentIssueId),
                AssigneeCycleTimeSeconds = GroupAverage(
                    completedSamples.Where(x => x.AssigneeId.HasValue && x.CycleTimeSeconds is > 0),
                    x => x.AssigneeId!.Value,
                    x => x.CycleTimeSeconds!.Value),
                ProjectCycleTimeSeconds = GroupAverage(
                    completedSamples.Where(x => x.CycleTimeSeconds is > 0),
                    x => x.ProjectId,
                    x => x.CycleTimeSeconds!.Value),
                WorkspaceCycleTimeSeconds = TrimmedAverage(
                    completedSamples
                        .Where(x => x.CycleTimeSeconds is > 0)
                        .Select(x => x.CycleTimeSeconds!.Value)),
                TypeAverageSeconds = GroupAverage(samples, x => x.Type),
                PriorityAverageSeconds = GroupAverage(samples, x => x.Priority),
            };
        }

        private static bool CanUseMl(List<IssueSample> samples)
        {
            return samples.Count >= MinMlTrainingSamples
                && samples.Select(x => RoundEstimateSeconds(x.ActualSeconds)).Distinct().Count() > 1;
        }

        private double? TryPredictWithMl(
            IssueEstimatePredictionPR request,
            ProjectPredictionScope project,
            List<IssueSample> samples,
            PredictionMetrics metrics)
        {
            try
            {
                var mlContext = new MLContext(seed: 4317);
                var rows = samples
                    .Select(x => ToMlRow(x, metrics, labelSeconds: x.ActualSeconds))
                    .ToList();
                var currentRow = ToMlRow(request, project.ProjectId, metrics);

                var dataView = mlContext.Data.LoadFromEnumerable(rows);
                var pipeline = mlContext.Transforms.Categorical.OneHotEncoding("TypeEncoded", nameof(IssueEstimateMlRow.Type))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding("StatusEncoded", nameof(IssueEstimateMlRow.Status)))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding("PriorityEncoded", nameof(IssueEstimateMlRow.Priority)))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding("ProjectEncoded", nameof(IssueEstimateMlRow.Project)))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding("AssigneeEncoded", nameof(IssueEstimateMlRow.Assignee)))
                    .Append(mlContext.Transforms.Concatenate(
                        "Features",
                        nameof(IssueEstimateMlRow.NameLength),
                        nameof(IssueEstimateMlRow.DescriptionLength),
                        nameof(IssueEstimateMlRow.HasParent),
                        nameof(IssueEstimateMlRow.HasAssignee),
                        nameof(IssueEstimateMlRow.ProjectAverageHours),
                        nameof(IssueEstimateMlRow.AssigneeAverageHours),
                        nameof(IssueEstimateMlRow.AssigneeRecentAverageHours),
                        nameof(IssueEstimateMlRow.AssigneeEstimateAccuracyRatio),
                        nameof(IssueEstimateMlRow.AssigneeThroughputPerWeek),
                        nameof(IssueEstimateMlRow.AssigneeOpenIssueCount),
                        nameof(IssueEstimateMlRow.AssigneeCycleTimeHours),
                        nameof(IssueEstimateMlRow.ProjectCycleTimeHours),
                        nameof(IssueEstimateMlRow.WorkspaceThroughputPerWeek),
                        nameof(IssueEstimateMlRow.WorkspaceAverageHours),
                        nameof(IssueEstimateMlRow.TypeAverageHours),
                        nameof(IssueEstimateMlRow.PriorityAverageHours),
                        "TypeEncoded",
                        "StatusEncoded",
                        "PriorityEncoded",
                        "ProjectEncoded",
                        "AssigneeEncoded"))
                    .Append(mlContext.Regression.Trainers.FastTree(
                        labelColumnName: nameof(IssueEstimateMlRow.Label),
                        featureColumnName: "Features",
                        numberOfLeaves: 16,
                        numberOfTrees: 120,
                        minimumExampleCountPerLeaf: samples.Count < 30 ? 1 : 3));

                var model = pipeline.Fit(dataView);
                var predictionEngine = mlContext.Model.CreatePredictionEngine<IssueEstimateMlRow, IssueEstimateMlPrediction>(model);
                var prediction = predictionEngine.Predict(currentRow);
                var predictedSeconds = Math.Exp(prediction.Score);

                return double.IsFinite(predictedSeconds) ? predictedSeconds : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML.NET estimate prediction failed. Falling back to heuristic prediction.");
                return null;
            }
        }

        private static IssueEstimateMlRow ToMlRow(IssueSample sample, PredictionMetrics metrics, double labelSeconds)
        {
            return new IssueEstimateMlRow
            {
                Label = (float)Math.Log(Math.Max(MinEstimateSeconds, labelSeconds)),
                Type = sample.Type.ToString(),
                Status = sample.Status.ToString(),
                Priority = sample.Priority.ToString(),
                Project = sample.ProjectId.ToString(),
                Assignee = sample.AssigneeId?.ToString() ?? "none",
                NameLength = NormalizeTextLength(sample.NameLength),
                DescriptionLength = NormalizeTextLength(sample.DescriptionLength),
                HasParent = sample.ParentId.HasValue ? 1 : 0,
                HasAssignee = sample.AssigneeId.HasValue ? 1 : 0,
                ProjectAverageHours = SecondsToHours(GetAverage(metrics.ProjectAverageSeconds, sample.ProjectId, metrics.WorkspaceAverageSeconds)),
                AssigneeAverageHours = SecondsToHours(GetAverage(metrics.AssigneeAverageSeconds, sample.AssigneeId, metrics.WorkspaceAverageSeconds)),
                AssigneeRecentAverageHours = SecondsToHours(GetAverage(
                    metrics.AssigneeRecentAverageSeconds,
                    sample.AssigneeId,
                    GetAverage(metrics.AssigneeAverageSeconds, sample.AssigneeId, metrics.WorkspaceAverageSeconds))),
                AssigneeEstimateAccuracyRatio = RatioToFloat(GetAverage(
                    metrics.AssigneeEstimateAccuracyRatio,
                    sample.AssigneeId,
                    GetWorkspaceEstimateAccuracyRatio(metrics))),
                AssigneeThroughputPerWeek = ThroughputToFloat(GetAverage(metrics.AssigneeThroughputPerWeek, sample.AssigneeId, 0)),
                AssigneeOpenIssueCount = OpenIssueCountToFloat(GetCount(metrics.AssigneeOpenIssueCounts, sample.AssigneeId)),
                AssigneeCycleTimeHours = SecondsToHours(GetAverage(
                    metrics.AssigneeCycleTimeSeconds,
                    sample.AssigneeId,
                    metrics.WorkspaceCycleTimeSeconds)),
                ProjectCycleTimeHours = SecondsToHours(GetAverage(
                    metrics.ProjectCycleTimeSeconds,
                    sample.ProjectId,
                    metrics.WorkspaceCycleTimeSeconds)),
                WorkspaceThroughputPerWeek = ThroughputToFloat(metrics.WorkspaceThroughputPerWeek),
                WorkspaceAverageHours = SecondsToHours(metrics.WorkspaceAverageSeconds),
                TypeAverageHours = SecondsToHours(GetAverage(metrics.TypeAverageSeconds, sample.Type, metrics.WorkspaceAverageSeconds)),
                PriorityAverageHours = SecondsToHours(GetAverage(metrics.PriorityAverageSeconds, sample.Priority, metrics.WorkspaceAverageSeconds)),
            };
        }

        private static IssueEstimateMlRow ToMlRow(
            IssueEstimatePredictionPR request,
            int projectId,
            PredictionMetrics metrics)
        {
            return new IssueEstimateMlRow
            {
                Type = request.Type.ToString(),
                Status = request.Status.ToString(),
                Priority = request.Priority.ToString(),
                Project = projectId.ToString(),
                Assignee = request.AssigneeId?.ToString() ?? "none",
                NameLength = NormalizeTextLength(request.Name?.Length ?? 0),
                DescriptionLength = NormalizeTextLength(request.Description?.Length ?? 0),
                HasParent = request.ParentId.HasValue ? 1 : 0,
                HasAssignee = request.AssigneeId.HasValue ? 1 : 0,
                ProjectAverageHours = SecondsToHours(GetAverage(metrics.ProjectAverageSeconds, projectId, metrics.WorkspaceAverageSeconds)),
                AssigneeAverageHours = SecondsToHours(GetAverage(metrics.AssigneeAverageSeconds, request.AssigneeId, metrics.WorkspaceAverageSeconds)),
                AssigneeRecentAverageHours = SecondsToHours(GetAverage(
                    metrics.AssigneeRecentAverageSeconds,
                    request.AssigneeId,
                    GetAverage(metrics.AssigneeAverageSeconds, request.AssigneeId, metrics.WorkspaceAverageSeconds))),
                AssigneeEstimateAccuracyRatio = RatioToFloat(GetAverage(
                    metrics.AssigneeEstimateAccuracyRatio,
                    request.AssigneeId,
                    GetWorkspaceEstimateAccuracyRatio(metrics))),
                AssigneeThroughputPerWeek = ThroughputToFloat(GetAverage(metrics.AssigneeThroughputPerWeek, request.AssigneeId, 0)),
                AssigneeOpenIssueCount = OpenIssueCountToFloat(GetCount(metrics.AssigneeOpenIssueCounts, request.AssigneeId)),
                AssigneeCycleTimeHours = SecondsToHours(GetAverage(
                    metrics.AssigneeCycleTimeSeconds,
                    request.AssigneeId,
                    metrics.WorkspaceCycleTimeSeconds)),
                ProjectCycleTimeHours = SecondsToHours(GetAverage(
                    metrics.ProjectCycleTimeSeconds,
                    projectId,
                    metrics.WorkspaceCycleTimeSeconds)),
                WorkspaceThroughputPerWeek = ThroughputToFloat(metrics.WorkspaceThroughputPerWeek),
                WorkspaceAverageHours = SecondsToHours(metrics.WorkspaceAverageSeconds),
                TypeAverageHours = SecondsToHours(GetAverage(metrics.TypeAverageSeconds, request.Type, metrics.WorkspaceAverageSeconds)),
                PriorityAverageHours = SecondsToHours(GetAverage(metrics.PriorityAverageSeconds, request.Priority, metrics.WorkspaceAverageSeconds)),
            };
        }

        private static HeuristicPrediction PredictByHeuristic(
            IssueEstimatePredictionPR request,
            PredictionMetrics metrics)
        {
            var candidates = new List<WeightedEstimate>();
            var defaultSeconds = GetDefaultSeconds(request.Type);
            var workspaceAverage = metrics.WorkspaceAverageSeconds > 0
                ? metrics.WorkspaceAverageSeconds
                : defaultSeconds;

            AddWeightedAverage(
                candidates,
                GetAverage(metrics.AssigneeAverageSeconds, request.AssigneeId, 0),
                0.35,
                "Assignee productivity");
            AddWeightedAverage(
                candidates,
                GetAverage(metrics.ProjectAverageSeconds, request.ProjectId, 0),
                0.25,
                "Project history");
            AddWeightedAverage(
                candidates,
                GetAverage(metrics.TypeAverageSeconds, request.Type, 0),
                0.2,
                "Issue type");
            AddWeightedAverage(
                candidates,
                GetAverage(metrics.PriorityAverageSeconds, request.Priority, 0),
                0.15,
                "Priority");
            AddWeightedAverage(candidates, workspaceAverage, 0.1, "Workspace history");
            AddWeightedAverage(candidates, defaultSeconds, 0.1, "Default baseline");

            var baseSeconds = candidates.Sum(x => x.Seconds * x.Weight) / candidates.Sum(x => x.Weight);
            var textLength = (request.Name?.Length ?? 0) + (request.Description?.Length ?? 0);
            var complexityMultiplier = 1 + Math.Min(0.35, Math.Sqrt(textLength) / 70);
            var hierarchyMultiplier = request.ParentId.HasValue ? 0.92 : 1;
            var assigneeMultiplier = request.AssigneeId.HasValue ? 1 : 1.08;
            var estimateAccuracyMultiplier = GetEstimateAccuracyMultiplier(request.AssigneeId, metrics);
            var throughputMultiplier = GetThroughputMultiplier(request.AssigneeId, metrics);
            var currentLoadMultiplier = GetCurrentLoadMultiplier(request.AssigneeId, metrics);
            var cycleTimeMultiplier = GetCycleTimeMultiplier(request.AssigneeId, request.ProjectId, metrics);
            var seconds = baseSeconds
                * complexityMultiplier
                * hierarchyMultiplier
                * assigneeMultiplier
                * estimateAccuracyMultiplier
                * throughputMultiplier
                * currentLoadMultiplier
                * cycleTimeMultiplier;

            return new HeuristicPrediction
            {
                Seconds = seconds,
                BaseSeconds = baseSeconds,
                ComplexityMultiplier = complexityMultiplier,
                HierarchyMultiplier = hierarchyMultiplier,
                AssigneeMultiplier = assigneeMultiplier,
                EstimateAccuracyMultiplier = estimateAccuracyMultiplier,
                ThroughputMultiplier = throughputMultiplier,
                CurrentLoadMultiplier = currentLoadMultiplier,
                CycleTimeMultiplier = cycleTimeMultiplier,
                Candidates = candidates,
            };
        }

        private static double GetEstimateAccuracyMultiplier(int? assigneeId, PredictionMetrics metrics)
        {
            var ratio = GetAverage(
                metrics.AssigneeEstimateAccuracyRatio,
                assigneeId,
                GetWorkspaceEstimateAccuracyRatio(metrics));

            return ratio > 0
                ? Math.Clamp(1 + (ratio - 1) * 0.12, 0.88, 1.18)
                : 1;
        }

        private static double GetThroughputMultiplier(int? assigneeId, PredictionMetrics metrics)
        {
            if (!assigneeId.HasValue
                || metrics.WorkspaceThroughputPerWeek <= 0
                || !metrics.AssigneeThroughputPerWeek.TryGetValue(assigneeId.Value, out var assigneeThroughput)
                || assigneeThroughput <= 0)
            {
                return 1;
            }

            var throughputRatio = assigneeThroughput / metrics.WorkspaceThroughputPerWeek;
            return Math.Clamp(1 - (throughputRatio - 1) * 0.06, 0.9, 1.1);
        }

        private static double GetCurrentLoadMultiplier(int? assigneeId, PredictionMetrics metrics)
        {
            var openIssueCount = GetCount(metrics.AssigneeOpenIssueCounts, assigneeId);
            return openIssueCount > 0
                ? 1 + Math.Min(0.1, openIssueCount * 0.01)
                : 1;
        }

        private static double GetCycleTimeMultiplier(int? assigneeId, int projectId, PredictionMetrics metrics)
        {
            if (!assigneeId.HasValue
                || !metrics.AssigneeCycleTimeSeconds.TryGetValue(assigneeId.Value, out var assigneeCycleTime)
                || !metrics.ProjectCycleTimeSeconds.TryGetValue(projectId, out var projectCycleTime)
                || assigneeCycleTime <= 0
                || projectCycleTime <= 0)
            {
                return 1;
            }

            var cycleTimeRatio = assigneeCycleTime / projectCycleTime;
            return Math.Clamp(1 + (cycleTimeRatio - 1) * 0.04, 0.92, 1.08);
        }

        private static List<IssueEstimatePredictionFactorModel> BuildFactors(
            IssueEstimatePredictionPR request,
            PredictionMetrics metrics,
            HeuristicPrediction heuristic,
            bool usedMlModel,
            int trainingSamples)
        {
            var factors = new List<IssueEstimatePredictionFactorModel>
            {
                new()
                {
                    Name = "Prediction basis",
                    Value = usedMlModel ? "Completed task history" : "Limited task history",
                    Description = usedMlModel
                        ? "The forecast is based on completed tasks with recorded time in this workspace."
                        : "There is little completed history, so the forecast relies more on project and workspace averages.",
                },
                new()
                {
                    Name = "Completed tasks analyzed",
                    Value = trainingSamples.ToString(),
                    Description = "Only issues with recorded time are included in the forecast.",
                },
            };

            if (request.AssigneeId.HasValue
                && metrics.AssigneeAverageSeconds.TryGetValue(request.AssigneeId.Value, out var assigneeAverage))
            {
                factors.Add(new IssueEstimatePredictionFactorModel
                {
                    Name = "Assignee productivity",
                    Value = FormatTimeTrackString(TimeSpan.FromSeconds(RoundEstimateSeconds(assigneeAverage))),
                    Description = "Average tracked time for tasks assigned to this user.",
                });
            }
            else
            {
                factors.Add(new IssueEstimatePredictionFactorModel
                {
                    Name = "Assignee productivity",
                    Value = "No personal history",
                    Description = "The prediction uses project and workspace history more strongly.",
                });
            }

            var productivityDetails = new List<string>();
            if (request.AssigneeId.HasValue
                && metrics.AssigneeRecentAverageSeconds.TryGetValue(request.AssigneeId.Value, out var recentAverage))
            {
                productivityDetails.Add($"recent avg {FormatTimeTrackString(TimeSpan.FromSeconds(RoundEstimateSeconds(recentAverage)))}");
            }

            if (request.AssigneeId.HasValue
                && metrics.AssigneeThroughputPerWeek.TryGetValue(request.AssigneeId.Value, out var throughput))
            {
                productivityDetails.Add($"{Math.Round(throughput, 1)} done/week");
            }

            var openIssueCount = GetCount(metrics.AssigneeOpenIssueCounts, request.AssigneeId);
            if (openIssueCount > 0)
            {
                productivityDetails.Add($"{openIssueCount} open");
            }

            if (productivityDetails.Count > 0)
            {
                factors.Add(new IssueEstimatePredictionFactorModel
                {
                    Name = "Recent productivity",
                    Value = string.Join(", ", productivityDetails),
                    Description = "Recent completed work, throughput and current load slightly adjust the prediction.",
                });
            }

            if (request.AssigneeId.HasValue
                && metrics.AssigneeEstimateAccuracyRatio.TryGetValue(request.AssigneeId.Value, out var estimateAccuracyRatio))
            {
                factors.Add(new IssueEstimatePredictionFactorModel
                {
                    Name = "Estimate accuracy",
                    Value = $"{Math.Round(estimateAccuracyRatio, 2)}x",
                    Description = "Historical actual time divided by previous estimates for this assignee.",
                });
            }

            if (request.AssigneeId.HasValue
                && metrics.AssigneeCycleTimeSeconds.TryGetValue(request.AssigneeId.Value, out var assigneeCycleTime))
            {
                factors.Add(new IssueEstimatePredictionFactorModel
                {
                    Name = "Cycle time",
                    Value = FormatTimeTrackString(TimeSpan.FromSeconds(RoundEstimateSeconds(assigneeCycleTime))),
                    Description = "Average calendar time from creation to completion for this assignee.",
                });
            }

            factors.Add(new IssueEstimatePredictionFactorModel
            {
                Name = "Project baseline",
                Value = FormatTimeTrackString(TimeSpan.FromSeconds(RoundEstimateSeconds(
                    GetAverage(metrics.ProjectAverageSeconds, request.ProjectId, heuristic.BaseSeconds)))),
                Description = "Average tracked time in the selected project.",
            });

            factors.Add(new IssueEstimatePredictionFactorModel
            {
                Name = "Task characteristics",
                Value = $"{request.Type}, {request.Priority}",
                Description = "Type, priority, parent task and text size adjust the baseline estimate.",
            });

            factors.Add(new IssueEstimatePredictionFactorModel
            {
                Name = "Text complexity",
                Value = $"{Math.Round(heuristic.ComplexityMultiplier, 2)}x",
                Description = "Longer names and descriptions usually indicate a larger task surface.",
            });

            return factors;
        }

        private static double BlendMlWithHeuristic(double mlSeconds, double heuristicSeconds, int sampleCount)
        {
            var mlWeight = sampleCount switch
            {
                >= 60 => 0.8,
                >= 30 => 0.7,
                >= MinMlTrainingSamples => 0.6,
                _ => 0.5,
            };

            return mlSeconds * mlWeight + heuristicSeconds * (1 - mlWeight);
        }

        private static double CalculateConfidence(
            int sampleCount,
            int? assigneeId,
            int projectId,
            PredictionMetrics metrics,
            bool usedMlModel)
        {
            var confidence = 0.25;
            confidence += Math.Min(0.35, sampleCount / 120.0);

            if (metrics.ProjectSampleCounts.TryGetValue(projectId, out var projectSamples))
            {
                confidence += Math.Min(0.12, projectSamples / 80.0);
            }

            if (assigneeId.HasValue && metrics.AssigneeSampleCounts.TryGetValue(assigneeId.Value, out var assigneeSamples))
            {
                confidence += Math.Min(0.15, assigneeSamples / 40.0);
            }

            if (assigneeId.HasValue && metrics.AssigneeRecentAverageSeconds.ContainsKey(assigneeId.Value))
            {
                confidence += 0.04;
            }

            if (assigneeId.HasValue && metrics.AssigneeEstimateAccuracyRatio.ContainsKey(assigneeId.Value))
            {
                confidence += 0.03;
            }

            if (assigneeId.HasValue && metrics.AssigneeThroughputPerWeek.ContainsKey(assigneeId.Value))
            {
                confidence += 0.03;
            }

            if (assigneeId.HasValue && metrics.AssigneeCycleTimeSeconds.ContainsKey(assigneeId.Value))
            {
                confidence += 0.02;
            }

            if (usedMlModel)
            {
                confidence += 0.08;
            }

            return Math.Round(Math.Min(0.95, confidence), 2);
        }

        private static Dictionary<TKey, double> GroupAverage<TKey>(
            IEnumerable<IssueSample> samples,
            Func<IssueSample, TKey> keySelector)
            where TKey : notnull
        {
            return samples
                .GroupBy(keySelector)
                .ToDictionary(x => x.Key, x => TrimmedAverage(x.Select(y => y.ActualSeconds)));
        }

        private static Dictionary<TKey, double> GroupAverage<TKey>(
            IEnumerable<IssueSample> samples,
            Func<IssueSample, TKey> keySelector,
            Func<IssueSample, double> valueSelector)
            where TKey : notnull
        {
            return samples
                .GroupBy(keySelector)
                .ToDictionary(x => x.Key, x => TrimmedAverage(x.Select(valueSelector)));
        }

        private static Dictionary<TKey, double> GroupThroughputPerWeek<TKey>(
            IEnumerable<IssueSample> samples,
            Func<IssueSample, TKey> keySelector,
            int windowDays)
            where TKey : notnull
        {
            var weeks = Math.Max(1, windowDays / 7.0);

            return samples
                .GroupBy(keySelector)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.Id).Distinct().Count() / weeks);
        }

        private static Dictionary<int, int> GroupOpenIssuesByAssignee(
            IEnumerable<IssueHistoryRow> issueRows,
            int? currentIssueId)
        {
            return issueRows
                .Where(x => !currentIssueId.HasValue || x.Id != currentIssueId.Value)
                .Where(x => x.AssigneeId.HasValue)
                .Where(x => !TerminalIssueStatuses.Contains(x.Status))
                .GroupBy(x => x.AssigneeId!.Value)
                .ToDictionary(x => x.Key, x => x.Count());
        }

        private static Dictionary<TKey, int> GroupCount<TKey>(
            IEnumerable<IssueSample> samples,
            Func<IssueSample, TKey> keySelector)
            where TKey : notnull
        {
            return samples
                .GroupBy(keySelector)
                .ToDictionary(x => x.Key, x => x.Count());
        }

        private static double TrimmedAverage(IEnumerable<double> values)
        {
            var ordered = values
                .Where(x => x > 0)
                .OrderBy(x => x)
                .ToList();

            if (ordered.Count == 0)
            {
                return 0;
            }

            var skip = ordered.Count >= 10 ? (int)Math.Floor(ordered.Count * 0.1) : 0;
            return ordered
                .Skip(skip)
                .Take(Math.Max(1, ordered.Count - skip * 2))
                .Average();
        }

        private static DateTime GetReferenceDate(IEnumerable<IssueSample> samples)
        {
            return samples
                .Select(x => x.LastActivityAt)
                .DefaultIfEmpty(DateTime.UtcNow)
                .Max();
        }

        private static double ClampEstimateAccuracyRatio(double ratio)
        {
            return Math.Clamp(ratio, MinEstimateAccuracyRatio, MaxEstimateAccuracyRatio);
        }

        private static void AddWeightedAverage(
            List<WeightedEstimate> candidates,
            double seconds,
            double weight,
            string source)
        {
            if (seconds <= 0)
            {
                return;
            }

            candidates.Add(new WeightedEstimate(seconds, weight, source));
        }

        private static double GetAverage<TKey>(
            IReadOnlyDictionary<TKey, double> values,
            TKey? key,
            double fallback)
            where TKey : struct
        {
            return key.HasValue && values.TryGetValue(key.Value, out var value) && value > 0
                ? value
                : fallback;
        }

        private static double GetAverage<TKey>(
            IReadOnlyDictionary<TKey, double> values,
            TKey key,
            double fallback)
            where TKey : notnull
        {
            return values.TryGetValue(key, out var value) && value > 0
                ? value
                : fallback;
        }

        private static int GetCount<TKey>(
            IReadOnlyDictionary<TKey, int> values,
            TKey? key)
            where TKey : struct
        {
            return key.HasValue && values.TryGetValue(key.Value, out var value) && value > 0
                ? value
                : 0;
        }

        private static double GetWorkspaceEstimateAccuracyRatio(PredictionMetrics metrics)
        {
            return metrics.WorkspaceEstimateAccuracyRatio > 0
                ? metrics.WorkspaceEstimateAccuracyRatio
                : 1;
        }

        private static double GetDefaultSeconds(IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => TimeSpan.FromHours(16).TotalSeconds,
                IssueType.Story => TimeSpan.FromHours(6).TotalSeconds,
                IssueType.Bug => TimeSpan.FromHours(3).TotalSeconds,
                _ => TimeSpan.FromHours(2).TotalSeconds,
            };
        }

        private static double ClampSeconds(double seconds)
        {
            return Math.Clamp(seconds, MinEstimateSeconds, MaxEstimateSeconds);
        }

        private static int RoundEstimateSeconds(double seconds)
        {
            var safeSeconds = Math.Max(MinEstimateSeconds, seconds);
            var step = safeSeconds switch
            {
                < 5 * 60 => 30,
                < 60 * 60 => 60,
                _ => 5 * 60,
            };

            return (int)Math.Max(MinEstimateSeconds, Math.Round(safeSeconds / step) * step);
        }

        private static float NormalizeTextLength(int length)
        {
            return Math.Min(length, 500) / 100f;
        }

        private static float SecondsToHours(double seconds)
        {
            return (float)(Math.Max(0, seconds) / 3600);
        }

        private static float RatioToFloat(double ratio)
        {
            return (float)ClampEstimateAccuracyRatio(ratio);
        }

        private static float ThroughputToFloat(double throughputPerWeek)
        {
            return (float)Math.Min(throughputPerWeek, 50);
        }

        private static float OpenIssueCountToFloat(int openIssueCount)
        {
            return Math.Min(openIssueCount, 50) / 10f;
        }

        private static string FormatTimeTrackString(TimeSpan value)
        {
            var totalSeconds = (int)Math.Max(MinEstimateSeconds, value.TotalSeconds);
            var hours = totalSeconds / 3600;
            var minutes = totalSeconds % 3600 / 60;
            var seconds = totalSeconds % 60;
            var parts = new List<string>();

            if (hours > 0)
            {
                parts.Add($"{hours}h");
            }

            if (minutes > 0)
            {
                parts.Add($"{minutes}m");
            }

            if (seconds > 0)
            {
                parts.Add($"{seconds}s");
            }

            return string.Join(" ", parts);
        }

        private sealed class ProjectPredictionScope
        {
            public int ProjectId { get; set; }

            public int WorkspaceId { get; set; }
        }

        private sealed class IssueHistoryRow
        {
            public int Id { get; set; }

            public DateTime ObjectCreateDate { get; set; }

            public DateTime ObjectEditDate { get; set; }

            public int NameLength { get; set; }

            public int DescriptionLength { get; set; }

            public IssueType Type { get; set; }

            public IssueStatus Status { get; set; }

            public IssuePriority Priority { get; set; }

            public TimeSpan? Estimate { get; set; }

            public int? ParentId { get; set; }

            public int? AssigneeId { get; set; }

            public int ProjectId { get; set; }
        }

        private sealed class TrackedTimeRow
        {
            public int IssueId { get; set; }

            public TimeSpan TimeSpent { get; set; }

            public DateTime DateBegin { get; set; }
        }

        private sealed class TrackedTimeSummary
        {
            public double Seconds { get; init; }

            public DateTime LastTrackedAt { get; init; }
        }

        private sealed class IssueCompletionRow
        {
            public int IssueId { get; set; }

            public DateTime ChangedAt { get; set; }
        }

        private sealed class IssueSample
        {
            public IssueSample(
                IssueHistoryRow issue,
                TrackedTimeSummary trackedTime,
                DateTime? completedAt)
            {
                Id = issue.Id;
                ObjectCreateDate = issue.ObjectCreateDate;
                ObjectEditDate = issue.ObjectEditDate;
                NameLength = issue.NameLength;
                DescriptionLength = issue.DescriptionLength;
                Type = issue.Type;
                Status = issue.Status;
                Priority = issue.Priority;
                EstimateSeconds = issue.Estimate?.TotalSeconds;
                ParentId = issue.ParentId;
                AssigneeId = issue.AssigneeId;
                ProjectId = issue.ProjectId;
                ActualSeconds = trackedTime.Seconds;
                LastActivityAt = trackedTime.LastTrackedAt > issue.ObjectEditDate
                    ? trackedTime.LastTrackedAt
                    : issue.ObjectEditDate;
                CompletedAt = completedAt;
                CycleTimeSeconds = completedAt.HasValue && completedAt.Value > issue.ObjectCreateDate
                    ? (completedAt.Value - issue.ObjectCreateDate).TotalSeconds
                    : null;
            }

            public int Id { get; }

            public DateTime ObjectCreateDate { get; }

            public DateTime ObjectEditDate { get; }

            public int NameLength { get; }

            public int DescriptionLength { get; }

            public IssueType Type { get; }

            public IssueStatus Status { get; }

            public IssuePriority Priority { get; }

            public double? EstimateSeconds { get; }

            public int? ParentId { get; }

            public int? AssigneeId { get; }

            public int ProjectId { get; }

            public double ActualSeconds { get; }

            public DateTime LastActivityAt { get; }

            public DateTime? CompletedAt { get; }

            public double? CycleTimeSeconds { get; }
        }

        private sealed class PredictionMetrics
        {
            public double WorkspaceAverageSeconds { get; init; }

            public Dictionary<int, double> ProjectAverageSeconds { get; init; } = [];

            public Dictionary<int, int> ProjectSampleCounts { get; init; } = [];

            public Dictionary<int, double> AssigneeAverageSeconds { get; init; } = [];

            public Dictionary<int, int> AssigneeSampleCounts { get; init; } = [];

            public Dictionary<int, double> AssigneeRecentAverageSeconds { get; init; } = [];

            public Dictionary<int, double> AssigneeEstimateAccuracyRatio { get; init; } = [];

            public double WorkspaceEstimateAccuracyRatio { get; init; }

            public Dictionary<int, double> AssigneeThroughputPerWeek { get; init; } = [];

            public double WorkspaceThroughputPerWeek { get; init; }

            public Dictionary<int, int> AssigneeOpenIssueCounts { get; init; } = [];

            public Dictionary<int, double> AssigneeCycleTimeSeconds { get; init; } = [];

            public Dictionary<int, double> ProjectCycleTimeSeconds { get; init; } = [];

            public double WorkspaceCycleTimeSeconds { get; init; }

            public Dictionary<IssueType, double> TypeAverageSeconds { get; init; } = [];

            public Dictionary<IssuePriority, double> PriorityAverageSeconds { get; init; } = [];
        }

        private sealed class HeuristicPrediction
        {
            public double Seconds { get; init; }

            public double BaseSeconds { get; init; }

            public double ComplexityMultiplier { get; init; }

            public double HierarchyMultiplier { get; init; }

            public double AssigneeMultiplier { get; init; }

            public double EstimateAccuracyMultiplier { get; init; }

            public double ThroughputMultiplier { get; init; }

            public double CurrentLoadMultiplier { get; init; }

            public double CycleTimeMultiplier { get; init; }

            public List<WeightedEstimate> Candidates { get; init; } = [];
        }

        private sealed record WeightedEstimate(double Seconds, double Weight, string Source);

        private sealed class IssueEstimateMlRow
        {
            public float Label { get; set; }

            public string Type { get; set; } = string.Empty;

            public string Status { get; set; } = string.Empty;

            public string Priority { get; set; } = string.Empty;

            public string Project { get; set; } = string.Empty;

            public string Assignee { get; set; } = string.Empty;

            public float NameLength { get; set; }

            public float DescriptionLength { get; set; }

            public float HasParent { get; set; }

            public float HasAssignee { get; set; }

            public float ProjectAverageHours { get; set; }

            public float AssigneeAverageHours { get; set; }

            public float AssigneeRecentAverageHours { get; set; }

            public float AssigneeEstimateAccuracyRatio { get; set; }

            public float AssigneeThroughputPerWeek { get; set; }

            public float AssigneeOpenIssueCount { get; set; }

            public float AssigneeCycleTimeHours { get; set; }

            public float ProjectCycleTimeHours { get; set; }

            public float WorkspaceThroughputPerWeek { get; set; }

            public float WorkspaceAverageHours { get; set; }

            public float TypeAverageHours { get; set; }

            public float PriorityAverageHours { get; set; }
        }

        private sealed class IssueEstimateMlPrediction
        {
            public float Score { get; set; }
        }
    }
}
