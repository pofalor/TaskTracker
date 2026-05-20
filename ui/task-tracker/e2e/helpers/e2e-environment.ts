const localHostnames = new Set(['localhost', '127.0.0.1', '::1']);

export function mutatingTestsAllowed(): boolean {
  if (process.env.E2E_ALLOW_MUTATION === '1') {
    return true;
  }

  const apiUrl = process.env.E2E_API_URL ?? 'https://localhost:44336';
  const appUrl = process.env.E2E_APP_URL ?? 'http://localhost:4200';

  return isLocalUrl(apiUrl) && isLocalUrl(appUrl);
}

export function mutationSkipReason(): string {
  return 'Mutating E2E tests are enabled only for localhost by default. Set E2E_ALLOW_MUTATION=1 for a disposable test environment.';
}

export function configuredReadOnlyCredentials(): {
  email: string;
  password: string;
  workspaceId: string;
  projectId: string;
} | undefined {
  const email = process.env.E2E_USER_EMAIL;
  const password = process.env.E2E_USER_PASSWORD;
  const workspaceId = process.env.E2E_WORKSPACE_ID;
  const projectId = process.env.E2E_PROJECT_ID;

  if (!email || !password || !workspaceId || !projectId) {
    return undefined;
  }

  return {
    email,
    password,
    workspaceId,
    projectId,
  };
}

function isLocalUrl(value: string): boolean {
  try {
    return localHostnames.has(new URL(value).hostname);
  } catch {
    return false;
  }
}
