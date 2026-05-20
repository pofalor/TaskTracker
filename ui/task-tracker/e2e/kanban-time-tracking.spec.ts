import { expect, test } from '@playwright/test';
import { mutationSkipReason, mutatingTestsAllowed } from './helpers/e2e-environment';
import { AutoTrackTimeStatus, TaskTrackerApi } from './helpers/task-tracker-api';
import { dismissTopModal, loginViaUi } from './helpers/ui-actions';

test.describe('Kanban and time tracking', () => {
  test.beforeEach(() => {
    test.skip(!mutatingTestsAllowed(), mutationSkipReason());
  });

  test('opens the Kanban board for a seeded project', async ({ page, request }) => {
    const api = new TaskTrackerApi(request);
    const scenario = await api.seedIssueScenario();

    await loginViaUi(page, scenario.user);
    await page.goto(`/all-issues?workspaceId=${scenario.workspace.id}&projectId=${scenario.project.id}`);

    await expect(page.getByTestId('kanban-board')).toBeVisible();
    await expect(page.getByTestId(`kanban-card-${scenario.issue.id}`)).toContainText(scenario.issue.name);
  });

  test('records manual time from the issue actions', async ({ page, request }) => {
    const api = new TaskTrackerApi(request);
    const scenario = await api.seedIssueScenario();

    await loginViaUi(page, scenario.user);
    await page.goto(`/all-issues?workspaceId=${scenario.workspace.id}&projectId=${scenario.project.id}`);
    await page.getByTestId(`kanban-track-time-${scenario.issue.id}`).click();
    await page.getByTestId('time-spent').fill('1h 20m');
    await page.getByTestId('time-date-begin').fill(new Date().toISOString().slice(0, 10));
    await page.getByTestId('time-comment').fill('manual e2e tracking');
    await page.getByTestId('time-save').click();
    await dismissTopModal(page);

    await page.getByTestId('table-view-button').click();
    await expect(page.getByTestId(`issue-row-${scenario.issue.id}`)).toContainText(/1:20:00|1h 20m|80m/i);
  });

  test('starts and stops automatic time tracking from the Kanban board', async ({ page, request }) => {
    const api = new TaskTrackerApi(request);
    const scenario = await api.seedIssueScenario();

    await loginViaUi(page, scenario.user);
    await page.goto(`/all-issues?workspaceId=${scenario.workspace.id}&projectId=${scenario.project.id}`);

    await page.getByTestId(`kanban-start-auto-track-${scenario.issue.id}`).click();
    await page.getByTestId('confirm-accept').click();
    await expect(page.getByText(/Active time track/i)).toBeVisible();

    await page.waitForTimeout(1_200);
    await page.getByTestId(`kanban-stop-auto-track-${scenario.issue.id}`).click();
    await page.getByTestId('confirm-accept').click();

    const activeTrack = await api.getActiveAutoTrack(scenario.token, scenario.project.id, scenario.workspace.id);
    await page.waitForTimeout(1_200);
    expect(activeTrack?.issueId).toBe(scenario.issue.id);
    expect(activeTrack?.autoTrackStatus).toBe(AutoTrackTimeStatus.Stopped);
    await expect.poll(async () => {
      const activeTrack = await api.getActiveAutoTrack(scenario.token, scenario.project.id, scenario.workspace.id);
      return activeTrack?.autoTrackStatus;
    }).toBe(AutoTrackTimeStatus.Stopped);

    const stoppedTrack = await api.getActiveAutoTrack(scenario.token, scenario.project.id, scenario.workspace.id);
      expect(stoppedTrack?.issueId).toBe(scenario.issue.id);
    });
});
