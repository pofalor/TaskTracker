import { expect, test } from '@playwright/test';
import { mutationSkipReason, mutatingTestsAllowed } from './helpers/e2e-environment';
import { createTestUser, TaskTrackerApi } from './helpers/task-tracker-api';
import { dismissTopModal, loginViaUi } from './helpers/ui-actions';

test.describe('Authentication', () => {
  test.beforeEach(() => {
    test.skip(!mutatingTestsAllowed(), mutationSkipReason());
  });

  test('registers a new user through the UI and opens workspaces', async ({ page }) => {
    const user = createTestUser();

    await page.goto('/register');
    await page.getByTestId('register-email').fill(user.email);
    await page.getByTestId('register-password').fill(user.password);
    await page.getByTestId('register-password-confirm').fill(user.password);
    await page.getByTestId('register-lastname').fill(user.lastName);
    await page.getByTestId('register-firstname').fill(user.firstName);
    await page.getByTestId('register-country').selectOption(String(user.country));
    await page.getByTestId('register-submit').click();

    await expect(page).toHaveURL(/\/my-workspaces/);
    await dismissTopModal(page);
    await expect(page.getByText(/My workspaces/i)).toBeVisible();
  });

  test('logs in through the UI with an existing API-created user', async ({ page, request }) => {
    const api = new TaskTrackerApi(request);
    const user = await api.registerUser();

    await loginViaUi(page, user);

    await expect(page.getByText(/My workspaces/i)).toBeVisible();
  });
});
