import { CommonModule } from '@angular/common';
import { Component, NgZone } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { BaseComponent } from '../../shared/base/base.component';
import { AuthService } from '../../shared/services/auth.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AuthenticatePostRequest } from '../../shared/model/postRequests/authenticatePostRequest';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent extends BaseComponent {
  public loginForm: FormGroup;
  errorText: string = '';

  constructor(
    public authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private modalService: NgbModal,
  ) {
    super(modalService);

    this.loginForm = fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
    });
  }

  async login() {
    let t = this;
    if (t.loginForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.loginForm);
      return;
    }
    t.setLoading(true);

    var loginCred: AuthenticatePostRequest = {
      email: t.loginForm.get('email')?.value,
      password: t.loginForm.get('password')?.value,
    }
    
    await t.authService.SignIn(loginCred)
      .then(() => {
        t.router.navigateByUrl('/dashboard');
      })
      .catch((e) => {
        if (!!e.error && !!e.error.error) {
          t.errorText = e.error.error_description;
        }
        else if(!!e.message){
          t.errorText = e.message;
        }
        else {
          t.errorText = 'System error. Please contact technical support.';
        }
      })
      .finally(() => {
        t.setLoading(false);
        localStorage.removeItem("Uuid");
      });
  }
}
