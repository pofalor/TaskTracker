import { IssueStatus } from "../enums/issue-status";

export var IssueStatuses = [
    { key : IssueStatus.Backlog, value : "Backlog"},
    { key : IssueStatus.SelectedForDevelopment, value : "Selected for development"},
    { key : IssueStatus.InProgress, value : "In progress"},
    { key : IssueStatus.PullRequest, value : "Pull request"},
    { key : IssueStatus.ToDeploy, value : "To deploy"},
    { key : IssueStatus.Test, value : "Test"},
    { key : IssueStatus.Declined, value : "Declined"},
    { key : IssueStatus.Done, value : "Done"},
    { key : IssueStatus.Deferred, value : "Deferred"}
  ]