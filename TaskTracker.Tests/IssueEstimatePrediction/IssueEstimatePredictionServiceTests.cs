using Microsoft.Extensions.Logging.Abstractions;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;
using TaskTracker.Core.src.Enums.ErrorCodes;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Services.Impl;
using Xunit;

namespace TaskTracker.Tests.IssueEstimatePrediction;

public sealed class IssueEstimatePredictionServiceTests
{
    [Fact]
    public async Task PredictEstimateAsync_RanksUsersByHistoricalSpeedAndCurrentLoad()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedPredictionWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var fastPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.FastUser.Id));
        var slowPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.SlowUser.Id));
        var loadedPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.LoadedUser.Id));

        Assert.True(fastPrediction.Success);
        Assert.True(slowPrediction.Success);
        Assert.True(loadedPrediction.Success);
        Assert.True(fastPrediction.Data.TrainingSamples >= 24);
        Assert.True(slowPrediction.Data.EstimateSeconds > fastPrediction.Data.EstimateSeconds * 2);
        Assert.True(loadedPrediction.Data.EstimateSeconds > fastPrediction.Data.EstimateSeconds);
        Assert.True(fastPrediction.Data.Confidence > 0.4);
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesColdStartFallbackForUserWithoutHistory()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedColdStartWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var prediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.NewUser.Id));

        Assert.True(prediction.Success);
        Assert.False(prediction.Data.UsedMlModel);
        Assert.Equal(0, prediction.Data.TrainingSamples);
        Assert.True(prediction.Data.EstimateSeconds > 0);
        Assert.True(prediction.Data.Confidence <= 0.35);
    }

    [Fact]
    public async Task PredictEstimateAsync_AdjustsForHistoricalEstimateAccuracy()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedEstimateAccuracyWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var accuratePrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.AccurateUser.Id));
        var underEstimatorPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.UnderEstimatorUser.Id));

        Assert.True(accuratePrediction.Success);
        Assert.True(underEstimatorPrediction.Success);
        Assert.True(underEstimatorPrediction.Data.EstimateSeconds > accuratePrediction.Data.EstimateSeconds);
        Assert.Contains(underEstimatorPrediction.Data.Factors, factor => factor.Name == "Estimate accuracy");
    }

    [Fact]
    public async Task PredictEstimateAsync_ReturnsValidationErrorForInvalidAssignee()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = CreateService(dbContext);
        var request = CreateRequest(projectId: 1, assigneeId: 0);

        var prediction = await service.PredictEstimateAsync(request);

        Assert.False(prediction.Success);
        Assert.Contains(prediction.Errors, error => error.Code == (int)IssueErrorCodes.IssueAssigneeInvalid);
    }

    [Fact]
    public async Task PredictEstimateAsync_ExcludesCurrentIssueFromTrainingSamples()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedCurrentIssueExclusionWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);
        var request = CreateRequest(seed.Project.Id, seed.User.Id);
        request.Id = seed.CurrentIssue.Id;

        var prediction = await service.PredictEstimateAsync(request);

        Assert.True(prediction.Success);
        Assert.Equal(1, prediction.Data.TrainingSamples);
        Assert.True(prediction.Data.EstimateSeconds < TimeSpan.FromHours(8).TotalSeconds);
    }

    [Fact]
    public async Task PredictEstimateAsync_IncreasesEstimateForLargerIssueTypeAndTextComplexity()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedIssueShapeWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);
        var bugRequest = CreateRequest(seed.Project.Id, seed.User.Id);
        bugRequest.Type = IssueType.Bug;
        bugRequest.Name = "Fix login redirect";
        bugRequest.Description = "Small defect with a known reproduction path.";
        var epicRequest = CreateRequest(seed.Project.Id, seed.User.Id);
        epicRequest.Type = IssueType.Epic;
        epicRequest.Name = "Implement cross-workspace planning and reporting";
        epicRequest.Description = string.Join(" ", Enumerable.Repeat(
            "Large feature area touching permissions, reporting, migrations, and user workflows.",
            8));

        var bugPrediction = await service.PredictEstimateAsync(bugRequest);
        var epicPrediction = await service.PredictEstimateAsync(epicRequest);

        Assert.True(bugPrediction.Success);
        Assert.True(epicPrediction.Success);
        Assert.False(bugPrediction.Data.UsedMlModel);
        Assert.False(epicPrediction.Data.UsedMlModel);
        Assert.Equal(8, epicPrediction.Data.TrainingSamples);
        Assert.True(epicPrediction.Data.EstimateSeconds > bugPrediction.Data.EstimateSeconds);
        Assert.Contains(epicPrediction.Data.Factors, factor => factor.Name == "Text complexity");
    }

    private static IssueEstimatePredictionService CreateService(ApplicationDbContext dbContext)
    {
        return new IssueEstimatePredictionService(
            dbContext,
            NullLogger<IssueEstimatePredictionService>.Instance);
    }

    private static IssueEstimatePredictionPR CreateRequest(int projectId, int assigneeId)
    {
        return new IssueEstimatePredictionPR
        {
            Name = "Implement automated testable workflow",
            Description = "A medium sized task with realistic user-facing behavior and persistence.",
            Type = IssueType.Task,
            Status = IssueStatus.Backlog,
            Priority = IssuePriority.Medium,
            AssigneeId = assigneeId,
            ProjectId = projectId,
        };
    }

    private static async Task<PredictionSeed> SeedPredictionWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("owner");
        var fastUser = CreateUser("fast");
        var slowUser = CreateUser("slow");
        var loadedUser = CreateUser("loaded");
        var workspace = new Workspace
        {
            Name = "Prediction ranking workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Prediction Ranking", "PRANK", owner, workspace);

        dbContext.AddRange(owner, fastUser, slowUser, loadedUser, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(fastUser, workspace, UserTeamRole.Developer),
            CreateMember(slowUser, workspace, UserTeamRole.Developer),
            CreateMember(loadedUser, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(dbContext, project, owner, fastUser, ref issueIndex, sampleCount: 8, actualHours: 1, estimateHours: 1);
        AddCompletedIssues(dbContext, project, owner, slowUser, ref issueIndex, sampleCount: 8, actualHours: 6, estimateHours: 2);
        AddCompletedIssues(dbContext, project, owner, loadedUser, ref issueIndex, sampleCount: 8, actualHours: 2, estimateHours: 2);
        AddOpenIssues(dbContext, project, owner, loadedUser, ref issueIndex, count: 12);

        await dbContext.SaveChangesAsync();

        return new PredictionSeed(project, fastUser, slowUser, loadedUser);
    }

    private static async Task<ColdStartSeed> SeedColdStartWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("cold-owner");
        var newUser = CreateUser("cold-new");
        var workspace = new Workspace
        {
            Name = "Cold start workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Cold Start", "COLD", owner, workspace);

        dbContext.AddRange(owner, newUser, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(newUser, workspace, UserTeamRole.Developer));

        await dbContext.SaveChangesAsync();

        return new ColdStartSeed(project, newUser);
    }

    private static async Task<EstimateAccuracySeed> SeedEstimateAccuracyWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("accuracy-owner");
        var accurateUser = CreateUser("accurate");
        var underEstimatorUser = CreateUser("under-estimator");
        var workspace = new Workspace
        {
            Name = "Estimate accuracy workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Estimate Accuracy", "EACCU", owner, workspace);

        dbContext.AddRange(owner, accurateUser, underEstimatorUser, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(accurateUser, workspace, UserTeamRole.Developer),
            CreateMember(underEstimatorUser, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(dbContext, project, owner, accurateUser, ref issueIndex, sampleCount: 12, actualHours: 3, estimateHours: 3);
        AddCompletedIssues(dbContext, project, owner, underEstimatorUser, ref issueIndex, sampleCount: 12, actualHours: 3, estimateHours: 1);

        await dbContext.SaveChangesAsync();

        return new EstimateAccuracySeed(project, accurateUser, underEstimatorUser);
    }

    private static async Task<CurrentIssueExclusionSeed> SeedCurrentIssueExclusionWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("current-owner");
        var user = CreateUser("current-user");
        var workspace = new Workspace
        {
            Name = "Current issue exclusion workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Current Issue Exclusion", "CIE", owner, workspace);

        dbContext.AddRange(owner, user, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(user, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(dbContext, project, owner, user, ref issueIndex, sampleCount: 1, actualHours: 2, estimateHours: 2);
        var currentIssue = CreateCompletedIssue(
            project,
            owner,
            user,
            issueIndex++,
            "Current long-running issue",
            actualHours: 24,
            estimateHours: 24);
        dbContext.Add(currentIssue.Issue);
        dbContext.Add(currentIssue.TimeTracking);
        dbContext.Add(currentIssue.StatusHistory);

        await dbContext.SaveChangesAsync();

        return new CurrentIssueExclusionSeed(project, user, currentIssue.Issue);
    }

    private static async Task<IssueShapeSeed> SeedIssueShapeWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("shape-owner");
        var user = CreateUser("shape-user");
        var workspace = new Workspace
        {
            Name = "Issue shape workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Issue Shape", "SHAPE", owner, workspace);

        dbContext.AddRange(owner, user, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(user, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(
            dbContext,
            project,
            owner,
            user,
            ref issueIndex,
            sampleCount: 4,
            actualHours: 2,
            estimateHours: 2,
            type: IssueType.Bug,
            priority: IssuePriority.Medium);
        AddCompletedIssues(
            dbContext,
            project,
            owner,
            user,
            ref issueIndex,
            sampleCount: 4,
            actualHours: 14,
            estimateHours: 14,
            type: IssueType.Epic,
            priority: IssuePriority.Medium);

        await dbContext.SaveChangesAsync();

        return new IssueShapeSeed(project, user);
    }

    private static User CreateUser(string key)
    {
        return new User
        {
            FirstName = "Test",
            LastName = key,
            Email = $"{key}-{Guid.NewGuid():N}@example.test",
            UserId = Guid.NewGuid().ToString(),
            Country = 0,
        };
    }

    private static WorkspaceMember CreateMember(User user, Workspace workspace, UserTeamRole role)
    {
        return new WorkspaceMember
        {
            User = user,
            Workspace = workspace,
            TeamRole = role,
            UserStatus = UserWorkspaceStatus.Active,
        };
    }

    private static Project CreateProject(string name, string code, User owner, Workspace workspace)
    {
        return new Project
        {
            Name = name,
            Description = $"{name} description",
            Code = code,
            StartDate = DateTime.UtcNow.Date.AddDays(-120),
            Author = owner,
            ProjectMgr = owner,
            Workspace = workspace,
        };
    }

    private static void AddCompletedIssues(
        ApplicationDbContext dbContext,
        Project project,
        User author,
        User assignee,
        ref int issueIndex,
        int sampleCount,
        double actualHours,
        double estimateHours,
        IssueType type = IssueType.Task,
        IssuePriority priority = IssuePriority.Medium)
    {
        for (var i = 0; i < sampleCount; i++)
        {
            var issue = new Issue
            {
                Name = $"{assignee.LastName} completed issue {i}",
                Description = "Historical completed issue for prediction training.",
                Type = type,
                Status = IssueStatus.Done,
                Priority = priority,
                Estimate = TimeSpan.FromHours(estimateHours),
                Index = issueIndex++,
                Author = author,
                Assignee = assignee,
                Project = project,
            };
            var trackedAt = DateTime.UtcNow.AddDays(-(i + 1));

            dbContext.Add(issue);
            dbContext.Add(new TimeTracking
            {
                Issue = issue,
                User = assignee,
                TimeSpent = TimeSpan.FromHours(actualHours),
                DateBegin = trackedAt,
                Comment = "Prediction training sample",
            });
            dbContext.Add(new IssueStatusHistory
            {
                Issue = issue,
                ChangedByUser = assignee,
                OldStatus = IssueStatus.InProgress,
                NewStatus = IssueStatus.Done,
                ChangedAt = trackedAt.AddHours(actualHours),
            });
        }
    }

    private static (Issue Issue, TimeTracking TimeTracking, IssueStatusHistory StatusHistory) CreateCompletedIssue(
        Project project,
        User author,
        User assignee,
        int issueIndex,
        string name,
        double actualHours,
        double estimateHours)
    {
        var issue = new Issue
        {
            Name = name,
            Description = "Historical completed issue for prediction training.",
            Type = IssueType.Task,
            Status = IssueStatus.Done,
            Priority = IssuePriority.Medium,
            Estimate = TimeSpan.FromHours(estimateHours),
            Index = issueIndex,
            Author = author,
            Assignee = assignee,
            Project = project,
        };
        var trackedAt = DateTime.UtcNow.AddDays(-1);

        return (
            issue,
            new TimeTracking
            {
                Issue = issue,
                User = assignee,
                TimeSpent = TimeSpan.FromHours(actualHours),
                DateBegin = trackedAt,
                Comment = "Prediction training sample",
            },
            new IssueStatusHistory
            {
                Issue = issue,
                ChangedByUser = assignee,
                OldStatus = IssueStatus.InProgress,
                NewStatus = IssueStatus.Done,
                ChangedAt = trackedAt.AddHours(actualHours),
            });
    }

    private static void AddOpenIssues(
        ApplicationDbContext dbContext,
        Project project,
        User author,
        User assignee,
        ref int issueIndex,
        int count)
    {
        for (var i = 0; i < count; i++)
        {
            dbContext.Add(new Issue
            {
                Name = $"{assignee.LastName} open issue {i}",
                Description = "Open issue used to model current workload.",
                Type = IssueType.Task,
                Status = IssueStatus.InProgress,
                Priority = IssuePriority.Medium,
                Index = issueIndex++,
                Author = author,
                Assignee = assignee,
                Project = project,
            });
        }
    }

    private sealed record PredictionSeed(Project Project, User FastUser, User SlowUser, User LoadedUser);

    private sealed record ColdStartSeed(Project Project, User NewUser);

    private sealed record EstimateAccuracySeed(Project Project, User AccurateUser, User UnderEstimatorUser);

    private sealed record CurrentIssueExclusionSeed(Project Project, User User, Issue CurrentIssue);

    private sealed record IssueShapeSeed(Project Project, User User);
}
