import { Routes } from '@angular/router';
import { UserGuard } from './shared/guards/user.guard';

export const routes: Routes = [
    { path: "", redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', loadComponent: () => import('./auth/login/login.component').then(c => c.LoginComponent) },
    { path: "register", loadComponent: () => import('./auth/register/register.component').then(c => c.RegisterComponent)  },
    { 
        path: "my-workspaces", 
        loadComponent: () => import('./components/my-workspaces/my-workspaces.component').then(c => c.MyWorkspacesComponent),
        canActivate: [UserGuard],
    },

    { path: "*", redirectTo: 'my-workspaces' },
];
