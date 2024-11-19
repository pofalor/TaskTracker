import { Routes } from '@angular/router';
import { RegisterComponent } from './auth/register/register.component';
import { LoginComponent } from './auth/login/login.component';

export const routes: Routes = [
    { path: "", redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: LoginComponent },
    { path: "register", component: RegisterComponent },
    //TODO: сделать переадресацию на организации
];
