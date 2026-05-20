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

    [Fact]
    public async Task PredictEstimateAsync_UsesIssueTypeHistory()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedIssueShapeWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);
        var bugRequest = CreateRequest(seed.Project.Id, seed.User.Id);
        bugRequest.Type = IssueType.Bug;
        var epicRequest = CreateRequest(seed.Project.Id, seed.User.Id);
        epicRequest.Type = IssueType.Epic;

        var bugPrediction = await service.PredictEstimateAsync(bugRequest);
        var epicPrediction = await service.PredictEstimateAsync(epicRequest);

        Assert.True(bugPrediction.Success);
        Assert.True(epicPrediction.Success);
        Assert.True(epicPrediction.Data.EstimateSeconds > bugPrediction.Data.EstimateSeconds);
        Assert.Contains(epicPrediction.Data.Factors, factor => factor.Name == "Task characteristics");
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesPriorityHistory()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedPriorityWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);
        var lowPriorityRequest = CreateRequest(seed.Project.Id, seed.User.Id);
        lowPriorityRequest.Priority = IssuePriority.Low;
        var highestPriorityRequest = CreateRequest(seed.Project.Id, seed.User.Id);
        highestPriorityRequest.Priority = IssuePriority.Highest;

        var lowPriorityPrediction = await service.PredictEstimateAsync(lowPriorityRequest);
        var highestPriorityPrediction = await service.PredictEstimateAsync(highestPriorityRequest);

        Assert.True(lowPriorityPrediction.Success);
        Assert.True(highestPriorityPrediction.Success);
        Assert.True(highestPriorityPrediction.Data.EstimateSeconds > lowPriorityPrediction.Data.EstimateSeconds);
        Assert.Contains(highestPriorityPrediction.Data.Factors, factor => factor.Name == "Task characteristics");
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesTextComplexity()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedColdStartWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);
        var simpleRequest = CreateRequest(seed.Project.Id, seed.NewUser.Id);
        simpleRequest.Name = "Fix typo";
        simpleRequest.Description = "Small text update.";
        var complexRequest = CreateRequest(seed.Project.Id, seed.NewUser.Id);
        complexRequest.Name = "Build detailed reporting pipeline with import validation and audit trail";
        complexRequest.Description = string.Join(" ", Enumerable.Repeat(
            "The task touches data import, validation, permissions, audit logging, reporting, and UI behavior.",
            12));

        var simplePrediction = await service.PredictEstimateAsync(simpleRequest);
        var complexPrediction = await service.PredictEstimateAsync(complexRequest);

        Assert.True(simplePrediction.Success);
        Assert.True(complexPrediction.Success);
        Assert.True(complexPrediction.Data.EstimateSeconds > simplePrediction.Data.EstimateSeconds);
        Assert.Contains(complexPrediction.Data.Factors, factor => factor.Name == "Text complexity");
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesParentIssueSignal()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedColdStartWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);
        var standaloneRequest = CreateRequest(seed.Project.Id, seed.NewUser.Id);
        var childRequest = CreateRequest(seed.Project.Id, seed.NewUser.Id);
        childRequest.ParentId = 42;

        var standalonePrediction = await service.PredictEstimateAsync(standaloneRequest);
        var childPrediction = await service.PredictEstimateAsync(childRequest);

        Assert.True(standalonePrediction.Success);
        Assert.True(childPrediction.Success);
        Assert.True(childPrediction.Data.EstimateSeconds < standalonePrediction.Data.EstimateSeconds);
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesProjectHistory()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedProjectBaselineWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var smallProjectPrediction = await service.PredictEstimateAsync(CreateRequest(seed.SmallProject.Id, seed.User.Id));
        var largeProjectPrediction = await service.PredictEstimateAsync(CreateRequest(seed.LargeProject.Id, seed.User.Id));

        Assert.True(smallProjectPrediction.Success);
        Assert.True(largeProjectPrediction.Success);
        Assert.True(largeProjectPrediction.Data.EstimateSeconds > smallProjectPrediction.Data.EstimateSeconds);
        Assert.Contains(largeProjectPrediction.Data.Factors, factor => factor.Name == "Project baseline");
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesAssigneeProductivity()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedPredictionWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var fastPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.FastUser.Id));
        var slowPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.SlowUser.Id));

        Assert.True(fastPrediction.Success);
        Assert.True(slowPrediction.Success);
        Assert.True(slowPrediction.Data.EstimateSeconds > fastPrediction.Data.EstimateSeconds);
        Assert.Contains(fastPrediction.Data.Factors, factor => factor.Name == "Assignee productivity");
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesCurrentOpenIssueLoad()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedCurrentLoadWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var freePrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.FreeUser.Id));
        var loadedPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.LoadedUser.Id));

        Assert.True(freePrediction.Success);
        Assert.True(loadedPrediction.Success);
        Assert.True(loadedPrediction.Data.EstimateSeconds > freePrediction.Data.EstimateSeconds);
        Assert.Contains(loadedPrediction.Data.Factors, factor =>
            factor.Name == "Recent productivity" && factor.Value.Contains("open"));
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesRecentThroughput()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedThroughputWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var highThroughputPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.HighThroughputUser.Id));
        var lowThroughputPrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.LowThroughputUser.Id));

        Assert.True(highThroughputPrediction.Success);
        Assert.True(lowThroughputPrediction.Success);
        Assert.True(highThroughputPrediction.Data.EstimateSeconds < lowThroughputPrediction.Data.EstimateSeconds);
        Assert.Contains(highThroughputPrediction.Data.Factors, factor =>
            factor.Name == "Recent productivity" && factor.Value.Contains("done/week"));
    }

    [Fact]
    public async Task PredictEstimateAsync_UsesCycleTime()
    {
        await using var database = await PredictionTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var seed = await SeedCycleTimeWorkspaceAsync(dbContext);
        var service = CreateService(dbContext);

        var shortCyclePrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.ShortCycleUser.Id));
        var longCyclePrediction = await service.PredictEstimateAsync(CreateRequest(seed.Project.Id, seed.LongCycleUser.Id));

        Assert.True(shortCyclePrediction.Success);
        Assert.True(longCyclePrediction.Success);
        Assert.True(longCyclePrediction.Data.EstimateSeconds > shortCyclePrediction.Data.EstimateSeconds);
        Assert.Contains(longCyclePrediction.Data.Factors, factor => factor.Name == "Cycle time");
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

    private static async Task<PrioritySeed> SeedPriorityWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("priority-owner");
        var user = CreateUser("priority-user");
        var workspace = new Workspace
        {
            Name = "Priority workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Priority", "PRIO", owner, workspace);

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
            priority: IssuePriority.Low);
        AddCompletedIssues(
            dbContext,
            project,
            owner,
            user,
            ref issueIndex,
            sampleCount: 4,
            actualHours: 10,
            estimateHours: 10,
            priority: IssuePriority.Highest);

        await dbContext.SaveChangesAsync();

        return new PrioritySeed(project, user);
    }

    private static async Task<ProjectBaselineSeed> SeedProjectBaselineWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("project-owner");
        var user = CreateUser("project-user");
        var workspace = new Workspace
        {
            Name = "Project baseline workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var smallProject = CreateProject("Small Project", "SMALL", owner, workspace);
        var largeProject = CreateProject("Large Project", "LARGE", owner, workspace);

        dbContext.AddRange(owner, user, workspace, smallProject, largeProject);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(user, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(dbContext, smallProject, owner, user, ref issueIndex, sampleCount: 8, actualHours: 2, estimateHours: 2);
        AddCompletedIssues(dbContext, largeProject, owner, user, ref issueIndex, sampleCount: 8, actualHours: 12, estimateHours: 12);

        await dbContext.SaveChangesAsync();

        return new ProjectBaselineSeed(smallProject, largeProject, user);
    }

    private static async Task<CurrentLoadSeed> SeedCurrentLoadWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("load-owner");
        var freeUser = CreateUser("free-load");
        var loadedUser = CreateUser("heavy-load");
        var workspace = new Workspace
        {
            Name = "Current load workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Current Load", "LOAD", owner, workspace);

        dbContext.AddRange(owner, freeUser, loadedUser, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(freeUser, workspace, UserTeamRole.Developer),
            CreateMember(loadedUser, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(dbContext, project, owner, freeUser, ref issueIndex, sampleCount: 6, actualHours: 4, estimateHours: 4);
        AddCompletedIssues(dbContext, project, owner, loadedUser, ref issueIndex, sampleCount: 6, actualHours: 4, estimateHours: 4);
        AddOpenIssues(dbContext, project, owner, loadedUser, ref issueIndex, count: 10);

        await dbContext.SaveChangesAsync();

        return new CurrentLoadSeed(project, freeUser, loadedUser);
    }

    private static async Task<ThroughputSeed> SeedThroughputWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("throughput-owner");
        var highThroughputUser = CreateUser("high-throughput");
        var lowThroughputUser = CreateUser("low-throughput");
        var workspace = new Workspace
        {
            Name = "Throughput workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Throughput", "THRU", owner, workspace);

        dbContext.AddRange(owner, highThroughputUser, lowThroughputUser, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(highThroughputUser, workspace, UserTeamRole.Developer),
            CreateMember(lowThroughputUser, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        AddCompletedIssues(dbContext, project, owner, highThroughputUser, ref issueIndex, sampleCount: 12, actualHours: 4, estimateHours: 4);
        AddCompletedIssues(dbContext, project, owner, lowThroughputUser, ref issueIndex, sampleCount: 2, actualHours: 4, estimateHours: 4);

        await dbContext.SaveChangesAsync();

        return new ThroughputSeed(project, highThroughputUser, lowThroughputUser);
    }

    private static async Task<CycleTimeSeed> SeedCycleTimeWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var owner = CreateUser("cycle-owner");
        var shortCycleUser = CreateUser("short-cycle");
        var longCycleUser = CreateUser("long-cycle");
        var workspace = new Workspace
        {
            Name = "Cycle time workspace",
            WorkspaceType = WorkspaceType.Personal,
            DirectorUser = owner,
        };
        var project = CreateProject("Cycle Time", "CYCLE", owner, workspace);

        dbContext.AddRange(owner, shortCycleUser, longCycleUser, workspace, project);
        dbContext.AddRange(
            CreateMember(owner, workspace, UserTeamRole.Owner),
            CreateMember(shortCycleUser, workspace, UserTeamRole.Developer),
            CreateMember(longCycleUser, workspace, UserTeamRole.Developer));

        var issueIndex = 1;
        var shortCycleIssues = AddCompletedIssues(
            dbContext,
            project,
            owner,
            shortCycleUser,
            ref issueIndex,
            sampleCount: 6,
            actualHours: 4,
            estimateHours: 4);
        var longCycleIssues = AddCompletedIssues(
            dbContext,
            project,
            owner,
            longCycleUser,
            ref issueIndex,
            sampleCount: 6,
            actualHours: 4,
            estimateHours: 4);

        await dbContext.SaveChangesAsync();

        ApplyCycleTime(shortCycleIssues, daysToComplete: 1);
        ApplyCycleTime(longCycleIssues, daysToComplete: 30);
        await dbContext.SaveChangesAsync();

        return new CycleTimeSeed(project, shortCycleUser, longCycleUser);
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

    private static List<CompletedIssueSeedItem> AddCompletedIssues(
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
        var issues = new List<CompletedIssueSeedItem>();

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

            var statusHistory = new IssueStatusHistory
            {
                Issue = issue,
                ChangedByUser = assignee,
                OldStatus = IssueStatus.InProgress,
                NewStatus = IssueStatus.Done,
                ChangedAt = trackedAt.AddHours(actualHours),
            };

            issues.Add(new CompletedIssueSeedItem(issue, statusHistory));
            dbContext.Add(issue);
            dbContext.Add(new TimeTracking
            {
                Issue = issue,
                User = assignee,
                TimeSpent = TimeSpan.FromHours(actualHours),
                DateBegin = trackedAt,
                Comment = "Prediction training sample",
            });
            dbContext.Add(statusHistory);
        }

        return issues;
    }

    private static void ApplyCycleTime(IEnumerable<CompletedIssueSeedItem> issues, int daysToComplete)
    {
        foreach (var item in issues)
        {
            var completedAt = DateTime.UtcNow.AddDays(-1);
            item.Issue.ObjectCreateDate = completedAt.AddDays(-daysToComplete);
            item.StatusHistory.ChangedAt = completedAt;
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

    private sealed record PrioritySeed(Project Project, User User);

    private sealed record ProjectBaselineSeed(Project SmallProject, Project LargeProject, User User);

    private sealed record CurrentLoadSeed(Project Project, User FreeUser, User LoadedUser);

    private sealed record ThroughputSeed(Project Project, User HighThroughputUser, User LowThroughputUser);

    private sealed record CycleTimeSeed(Project Project, User ShortCycleUser, User LongCycleUser);

    private sealed record CompletedIssueSeedItem(Issue Issue, IssueStatusHistory StatusHistory);
}
