import { Routes } from '@angular/router';
import { UserGuard } from './shared/guards/user.guard';

export const routes: Routes = [
    { path: "", redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', loadComponent: () => import('./auth/login/login.component').then(c => c.LoginComponent) },
    { path: "register", loadComponent: () => import('./auth/register/register.component').then(c => c.RegisterComponent)  },
    { 
        path: "organisations", 
        loadComponent: () => import('./components/all-organisations/all-organisations.component').then(c => c.AllOrganisationsComponent),
        canActivate: [UserGuard],
    },

    { path: "*", redirectTo: 'organisations' },
];
