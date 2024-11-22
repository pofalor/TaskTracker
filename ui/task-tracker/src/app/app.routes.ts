import { Routes } from '@angular/router';
import { RegisterComponent } from './auth/register/register.component';
import { LoginComponent } from './auth/login/login.component';
import { AllOrganisationsComponent } from './components/all-organisations/all-organisations.component';

export const routes: Routes = [
    { path: "", redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: LoginComponent },
    { path: "register", component: RegisterComponent },
    { path: "organisations", component: AllOrganisationsComponent },

    { path: "**", redirectTo: 'organisations' },
];
