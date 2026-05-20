import { APIRequestContext, APIResponse, expect } from '@playwright/test';

export const IssueType = {
  Task: 0,
  Story: 1,
  Bug: 2,
  Epic: 3,
} as const;

export const IssueStatus = {
  Backlog: 0,
  SelectedForDevelopment: 1,
  InProgress: 2,
  PullRequest: 3,
  ToDeploy: 4,
  Test: 5,
  Declined: 6,
  Done: 7,
  Deferred: 8,
} as const;

export const IssuePriority = {
  Lowest: 0,
  Low: 1,
  Medium: 2,
  High: 3,
  Highest: 4,
} as const;

export const AutoTrackTimeStatus = {
  Active: 0,
  Stopped: 1,
  Finished: 2,
} as const;

type ApiError = {
  code: number;
  message: string;
};

type ApiResponse<T> = {
  data: T;
  errors?: ApiError[];
};

export type TestUser = {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  country: number;
};

export type UserModel = {
  id: number;
  name: string;
  email: string;
  roles: string[];
  country?: number;
};

export type WorkspaceModel = {
  id: number;
  name: string;
  workspaceType: number;
  directorUserId: number;
};

export type ProjectModel = {
  id: number;
  name: string;
  description: string;
  code: string;
  startDate: string;
  authorId: number;
  projectMgrId: number;
  workspaceId: number;
};

export type IssueModel = {
  id: number;
  projectCode: string;
  name: string;
  description: string;
  type: number;
  status: number;
  priority: number;
  estimate?: string;
  index: number;
  authorId: number;
  assigneeId?: number;
  projectId: number;
  timeTrack?: string;
};

export type TimeTrackingModel = {
  id?: number;
  timeSpent: string;
  dateBegin: string;
  comment?: string;
  autoTrackStatus?: number;
  userId: number;
  issueId: number;
};

export type EstimatePredictionModel = {
  estimateSeconds: number;
  estimate: string;
  usedMlModel: boolean;
  trainingSamples: number;
  confidence: number;
};

export type SeededIssueScenario = {
  user: TestUser;
  token: string;
  currentUser: UserModel;
  workspace: WorkspaceModel;
  project: ProjectModel;
  issue: IssueModel;
};

const defaultApiUrl = 'https://localhost:44336';

export function uniqueRunId(prefix = 'e2e'): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

export function createTestUser(runId = uniqueRunId()): TestUser {
  return {
    email: `${runId}@example.test`,
    password: 'Aa123456!',
    firstName: 'Etest',
    lastName: 'User',
    country: 0,
  };
}

export class TaskTrackerApi {
  private readonly apiUrl: string;

  constructor(private readonly request: APIRequestContext) {
    this.apiUrl = (process.env.E2E_API_URL ?? defaultApiUrl).replace(/\/+$/, '');
  }

  async registerUser(user = createTestUser()): Promise<TestUser> {
    await this.postAnonymous<boolean>('api/auth/register', {
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      password: user.password,
      country: user.country,
    });

    return user;
  }

  async authenticate(user: Pick<TestUser, 'email' | 'password'>): Promise<string> {
    const auth = await this.postAnonymous<{ token: string }>('api/auth/authenticate', {
      email: user.email,
      password: user.password,
    });

    expect(auth.token, 'Authentication response should contain JWT token').toBeTruthy();
    return auth.token;
  }

  async getCurrentUser(token: string): Promise<UserModel> {
    return this.get<UserModel>('api/user/getUser', token);
  }

  async createWorkspace(token: string, name = uniqueRunId('workspace')): Promise<WorkspaceModel> {
    await this.post<boolean>('api/workspace/add', {
      name,
      workspaceType: 0,
    }, token);

    const workspaces = await this.get<WorkspaceModel[]>('api/workspace/getMyWorkspaces', token);
    const workspace = workspaces.find((item) => item.name === name);
    expect(workspace, `Workspace "${name}" should be returned by API`).toBeTruthy();
    return workspace!;
  }

  async createProject(
    token: string,
    workspaceId: number,
    projectMgrId: number,
    name = uniqueRunId('project'),
  ): Promise<ProjectModel> {
    const code = name.replace(/[^a-z0-9]/gi, '').slice(0, 5).toUpperCase() || 'E2E';

    await this.post<boolean>('api/project/add', {
      name,
      description: `${name} description`,
      code,
      startDate: todayForInput(),
      projectMgrId,
      workspaceId,
    }, token);

    const projects = await this.get<ProjectModel[]>(`api/project/getWorkspaceProjects?workspaceId=${workspaceId}`, token);
    const project = projects.find((item) => item.name === name);
    expect(project, `Project "${name}" should be returned by API`).toBeTruthy();
    return project!;
  }

  async createIssue(
    token: string,
    workspaceId: number,
    projectId: number,
    userId: number,
    overrides: Partial<{
      name: string;
      description: string;
      type: number;
      status: number;
      priority: number;
      estimate: string;
      assigneeId: number;
    }> = {},
  ): Promise<IssueModel> {
    const name = overrides.name ?? uniqueRunId('issue');

    await this.post<boolean>('api/issue/create', {
      name,
      description: overrides.description ?? `${name} description`,
      type: overrides.type ?? IssueType.Task,
      status: overrides.status ?? IssueStatus.Backlog,
      priority: overrides.priority ?? IssuePriority.Medium,
      estimate: overrides.estimate ?? '1h',
      authorId: userId,
      assigneeId: overrides.assigneeId ?? userId,
      projectId,
    }, token);

    const issues = await this.getProjectIssues(token, workspaceId, projectId);
    const issue = issues.find((item) => item.name === name);
    expect(issue, `Issue "${name}" should be returned by API`).toBeTruthy();
    return issue!;
  }

  async getProjectIssues(token: string, workspaceId: number, projectId: number): Promise<IssueModel[]> {
    return this.post<IssueModel[]>('api/issue/getProjectIssues', {
      workspaceId,
      projectId,
    }, token);
  }

  async trackTime(token: string, issueId: number, timeSpent = '1h 15m', comment = uniqueRunId('manual-track')): Promise<boolean> {
    return this.post<boolean>('api/issue/trackTime', {
      issueId,
      timeSpent,
      dateBegin: todayForInput(),
      comment,
    }, token);
  }

  async startAutoTracking(token: string, userId: number, issueId: number): Promise<TimeTrackingModel> {
    return this.post<TimeTrackingModel>('api/autotrack/startTracking', {
      issueId,
      userId,
      timeSpent: '0h 0m 0s',
      dateBegin: new Date().toISOString().split('.')[0],
    }, token);
  }

  async stopAutoTracking(token: string, issueId: number, activeTrackId: number): Promise<TimeTrackingModel> {
    return this.post<TimeTrackingModel>('api/autotrack/stopTracking', {
      id: activeTrackId,
      issueId,
      timeSpent: '0h 0m 2s',
    }, token);
  }

  async getActiveAutoTrack(token: string, projectId: number, workspaceId: number): Promise<TimeTrackingModel | null> {
    return this.get<TimeTrackingModel | null>(
      `api/autotrack/getActiveAutoTrack?projectId=${projectId}&workspaceId=${workspaceId}`,
      token,
    );
  }

  async predictEstimate(token: string, projectId: number, assigneeId: number): Promise<EstimatePredictionModel> {
    return this.post<EstimatePredictionModel>('api/issue/predictEstimate', {
      name: uniqueRunId('forecast'),
      description: 'Estimate prediction smoke request',
      type: IssueType.Task,
      status: IssueStatus.Backlog,
      priority: IssuePriority.Medium,
      assigneeId,
      projectId,
    }, token);
  }

  async seedIssueScenario(overrides: Partial<{ issueStatus: number }> = {}): Promise<SeededIssueScenario> {
    const user = await this.registerUser();
    const token = await this.authenticate(user);
    const currentUser = await this.getCurrentUser(token);
    const workspace = await this.createWorkspace(token);
    const project = await this.createProject(token, workspace.id, currentUser.id);
    const issue = await this.createIssue(token, workspace.id, project.id, currentUser.id, {
      status: overrides.issueStatus ?? IssueStatus.InProgress,
    });

    return {
      user,
      token,
      currentUser,
      workspace,
      project,
      issue,
    };
  }

  private async get<T>(path: string, token: string): Promise<T> {
    const response = await this.request.get(this.url(path), {
      headers: this.authHeaders(token),
      ignoreHTTPSErrors: true,
    });

    return this.readResponse<T>(response, path);
  }

  private async post<T>(path: string, data: unknown, token: string): Promise<T> {
    const response = await this.request.post(this.url(path), {
      data,
      headers: this.authHeaders(token),
      ignoreHTTPSErrors: true,
    });

    return this.readResponse<T>(response, path);
  }

  private async postAnonymous<T>(path: string, data: unknown): Promise<T> {
    const response = await this.request.post(this.url(path), {
      data,
      headers: this.publicHeaders(),
      ignoreHTTPSErrors: true,
    });

    return this.readResponse<T>(response, path);
  }

  private async readResponse<T>(response: APIResponse, path: string): Promise<T> {
    expect(response.ok(), `${path} should return a successful HTTP status`).toBeTruthy();
    const json = await response.json() as ApiResponse<T>;
    expect(json.errors ?? [], `${path} should not return API errors`).toHaveLength(0);
    return json.data;
  }

  private url(path: string): string {
    return `${this.apiUrl}/${path.replace(/^\/+/, '')}`;
  }

  private publicHeaders(): Record<string, string> {
    return {
      'Content-Type': 'application/json',
      Localization: 'en',
      Uuid: uniqueRunId('uuid'),
    };
  }

  private authHeaders(token: string): Record<string, string> {
    return {
      ...this.publicHeaders(),
      Authorization: `Bearer ${token}`,
    };
  }
}

export function todayForInput(): string {
  return new Date().toISOString().slice(0, 10);
}
