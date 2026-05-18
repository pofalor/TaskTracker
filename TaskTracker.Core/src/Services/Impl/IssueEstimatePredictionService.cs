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
                var trackedSecondsByIssue = await LoadTrackedSecondsByIssueAsync(project.WorkspaceId);
                var samples = BuildSamples(issueRows, trackedSecondsByIssue, request.Id);
                var metrics = BuildMetrics(samples);
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
                    NameLength = x.Name.Length,
                    DescriptionLength = x.Description.Length,
                    Type = x.Type,
                    Status = x.Status,
                    Priority = x.Priority,
                    ParentId = x.ParentId,
                    AssigneeId = x.AssigneeId,
                    ProjectId = x.ProjectId,
                })
                .ToListAsync();
        }

        private async Task<Dictionary<int, double>> LoadTrackedSecondsByIssueAsync(int workspaceId)
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
                })
                .ToListAsync();

            return timeRows
                .GroupBy(x => x.IssueId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(y => y.TimeSpent.TotalSeconds));
        }

        private static List<IssueSample> BuildSamples(
            IEnumerable<IssueHistoryRow> issueRows,
            IReadOnlyDictionary<int, double> trackedSecondsByIssue,
            int? currentIssueId)
        {
            return issueRows
                .Where(x => !currentIssueId.HasValue || x.Id != currentIssueId.Value)
                .Select(x => trackedSecondsByIssue.TryGetValue(x.Id, out var seconds)
                    ? new IssueSample(x, seconds)
                    : null)
                .Where(x => x is { ActualSeconds: > 0 })
                .Select(x => x!)
                .ToList();
        }

        private static PredictionMetrics BuildMetrics(List<IssueSample> samples)
        {
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
            var seconds = baseSeconds * complexityMultiplier * hierarchyMultiplier * assigneeMultiplier;

            return new HeuristicPrediction
            {
                Seconds = seconds,
                BaseSeconds = baseSeconds,
                ComplexityMultiplier = complexityMultiplier,
                HierarchyMultiplier = hierarchyMultiplier,
                AssigneeMultiplier = assigneeMultiplier,
                Candidates = candidates,
            };
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
                    Name = "Model",
                    Value = usedMlModel ? "ML.NET FastTree" : "Heuristic fallback",
                    Description = usedMlModel
                        ? "The model was trained on completed time-tracking history in this workspace."
                        : "There is not enough varied history for ML.NET, so the prediction uses weighted historical averages.",
                },
                new()
                {
                    Name = "Training samples",
                    Value = trainingSamples.ToString(),
                    Description = "Only issues with recorded time are used as training examples.",
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

            public int NameLength { get; set; }

            public int DescriptionLength { get; set; }

            public IssueType Type { get; set; }

            public IssueStatus Status { get; set; }

            public IssuePriority Priority { get; set; }

            public int? ParentId { get; set; }

            public int? AssigneeId { get; set; }

            public int ProjectId { get; set; }
        }

        private sealed class TrackedTimeRow
        {
            public int IssueId { get; set; }

            public TimeSpan TimeSpent { get; set; }
        }

        private sealed class IssueSample
        {
            public IssueSample(IssueHistoryRow issue, double actualSeconds)
            {
                Id = issue.Id;
                NameLength = issue.NameLength;
                DescriptionLength = issue.DescriptionLength;
                Type = issue.Type;
                Status = issue.Status;
                Priority = issue.Priority;
                ParentId = issue.ParentId;
                AssigneeId = issue.AssigneeId;
                ProjectId = issue.ProjectId;
                ActualSeconds = actualSeconds;
            }

            public int Id { get; }

            public int NameLength { get; }

            public int DescriptionLength { get; }

            public IssueType Type { get; }

            public IssueStatus Status { get; }

            public IssuePriority Priority { get; }

            public int? ParentId { get; }

            public int? AssigneeId { get; }

            public int ProjectId { get; }

            public double ActualSeconds { get; }
        }

        private sealed class PredictionMetrics
        {
            public double WorkspaceAverageSeconds { get; init; }

            public Dictionary<int, double> ProjectAverageSeconds { get; init; } = [];

            public Dictionary<int, int> ProjectSampleCounts { get; init; } = [];

            public Dictionary<int, double> AssigneeAverageSeconds { get; init; } = [];

            public Dictionary<int, int> AssigneeSampleCounts { get; init; } = [];

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
