import { expect, test } from '@playwright/test';
import { configuredReadOnlyCredentials } from './helpers/e2e-environment';
import { loginViaUi } from './helpers/ui-actions';

test.describe('Read-only smoke', () => {
  test('logs in and opens a configured Kanban board without creating data', async ({ page }) => {
    const credentials = configuredReadOnlyCredentials();
    test.skip(
      !credentials,
      'Set E2E_USER_EMAIL, E2E_USER_PASSWORD, E2E_WORKSPACE_ID and E2E_PROJECT_ID to run read-only smoke tests.',
    );

    await loginViaUi(page, credentials!);
    await page.goto(`/all-issues?workspaceId=${credentials!.workspaceId}&projectId=${credentials!.projectId}`);

    await expect(page.getByText(/All issues/i)).toBeVisible();
    await page.getByTestId('kanban-view-button').click();
    await expect(page.getByTestId('kanban-board').or(page.getByText(/does not have any issues/i))).toBeVisible();
  });
});
