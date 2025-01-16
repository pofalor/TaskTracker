import { CommonModule } from '@angular/common';
import { Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { BaseComponent } from '../../shared/base/base.component';
import { AuthService } from '../../shared/services/onlyFrontServices/auth.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AuthenticatePostRequest } from '../../shared/model/postRequests/authenticatePostRequest';
import { UserService } from '../../shared/services/user.service';
import { LangPipe } from '../../shared/pipes/lang.pipe';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-login',
    imports: [CommonModule, RouterLink, ReactiveFormsModule, LangPipe],
    templateUrl: './login.component.html',
    styleUrl: './login.component.scss'
})
export class LoginComponent extends BaseComponent implements OnInit {
  public loginForm: FormGroup;
  errorText: string = '';

  constructor(
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private modalService: NgbModal,
    private userService: UserService,
    private translate: TranslateService
  ) {
    super(modalService, translate);

    this.loginForm = fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
    });
  }

  ngOnInit() {
    var t = this;
    if(t.authService.isLoggedIn){
      t.authService.SignOut();
    }
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
        t.userService.init();
        t.router.navigateByUrl('/my-workspaces');
      })
      .catch((e) => {
        if (!!e.error && !!e.error.errors && !!e.error.errors[0].message) {
          t.errorText = e.error.errors[0].message;
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
        localStorage?.removeItem("Uuid");
      });
  }
}
