import { Routes } from '@angular/router';
import { UserGuard } from './shared/guards/user.guard';

export const routes: Routes = [
    { path: "", redirectTo: 'my-workspaces', pathMatch: 'full' },
    { path: 'login', loadComponent: () => import('./auth/login/login.component').then(c => c.LoginComponent) },
    { path: "register", loadComponent: () => import('./auth/register/register.component').then(c => c.RegisterComponent)  },
    { 
        path: "my-workspaces", 
        loadComponent: () => import('./components/my-workspaces/my-workspaces.component').then(c => c.MyWorkspacesComponent),
        canActivate: [UserGuard],
    },
    { 
        path: "workspace-info",
        loadComponent: () => import('./components/workspace-info/workspace-info.component').then(c => c.WorkspaceInfoComponent),
        canActivate: [UserGuard],
    },
    { 
        path: "all-issues",
        loadComponent: () => import('./components/all-issues/all-issues.component').then(c => c.AllIssuesComponent),
        canActivate: [UserGuard],
    },

    { path: "*", redirectTo: 'my-workspaces' },
];
