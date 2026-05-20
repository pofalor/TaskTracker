import { expect, Page } from '@playwright/test';
import { TestUser } from './task-tracker-api';

export async function loginViaUi(page: Page, user: Pick<TestUser, 'email' | 'password'>): Promise<void> {
  await page.goto('/login');
  await page.getByTestId('login-email').fill(user.email);
  await page.getByTestId('login-password').fill(user.password);
  await page.getByTestId('login-submit').click();
  await expect(page).toHaveURL(/\/my-workspaces/);
}

export async function dismissTopModal(page: Page): Promise<void> {
  const confirm = page.getByTestId('confirm-accept').last();
  if (await confirm.waitFor({ state: 'visible', timeout: 2_000 }).then(() => true).catch(() => false)) {
    await confirm.click();
  }
}

export async function selectFirstNgOption(page: Page, testId: string): Promise<void> {
  await page.getByTestId(testId).click();
  await page.locator('.ng-option').first().click();
}
