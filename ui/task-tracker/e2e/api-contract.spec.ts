import { expect, test } from '@playwright/test';
import { mutationSkipReason, mutatingTestsAllowed } from './helpers/e2e-environment';
import {
  AutoTrackTimeStatus,
  IssueStatus,
  TaskTrackerApi,
} from './helpers/task-tracker-api';

test.describe('API contracts', () => {
  test.beforeEach(() => {
    test.skip(!mutatingTestsAllowed(), mutationSkipReason());
  });

  test('covers auth, board data, manual tracking, auto tracking and estimate prediction endpoints', async ({ request }) => {
    const api = new TaskTrackerApi(request);
    const scenario = await api.seedIssueScenario({ issueStatus: IssueStatus.InProgress });

    const issuesBeforeTracking = await api.getProjectIssues(scenario.token, scenario.workspace.id, scenario.project.id);
    expect(issuesBeforeTracking.some((issue) => issue.id === scenario.issue.id)).toBeTruthy();

    await expect(api.trackTime(scenario.token, scenario.issue.id, '35m', 'api manual tracking')).resolves.toBeTruthy();

    const activeTrack = await api.startAutoTracking(scenario.token, scenario.currentUser.id, scenario.issue.id);
    expect(activeTrack.issueId).toBe(scenario.issue.id);
    expect(activeTrack.autoTrackStatus).toBe(AutoTrackTimeStatus.Active);

    const stoppedTrack = await api.stopAutoTracking(scenario.token, scenario.issue.id, activeTrack.id!);
    expect(stoppedTrack.autoTrackStatus).toBe(AutoTrackTimeStatus.Stopped);

    const prediction = await api.predictEstimate(scenario.token, scenario.project.id, scenario.currentUser.id);
    expect(prediction.estimateSeconds).toBeGreaterThan(0);
    expect(prediction.confidence).toBeGreaterThan(0);
  });
});
