import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { BaseComponent } from '../../shared/base/base.component';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { AuthService } from '../../shared/services/onlyFrontServices/auth.service';
import { ValidationUtils } from '../../shared/utils/validationUtils';
import { PublicService } from '../../shared/services/public.service';
import { AuthenticatePostRequest } from '../../shared/model/postRequests/authenticatePostRequest';
import { CreateUserPostRequest } from '../../shared/model/postRequests/createUserPostRequest';
import { ListCountry } from '../../shared/constants/country';
import { UserService } from '../../shared/services/user.service';
import { LangPipe } from '../../shared/pipes/lang.pipe';
import { TranslateService } from '@ngx-translate/core';

@Component({
    selector: 'app-register',
    imports: [CommonModule, RouterLink, ReactiveFormsModule, LangPipe],
    templateUrl: './register.component.html',
    styleUrl: './register.component.scss'
})
export class RegisterComponent extends BaseComponent {
  public registerForm: FormGroup;
  public countryList: any[] = ListCountry;
  selectedCountry: any;

  constructor(
    public authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private modalService: NgbModal,
    private publicService: PublicService,
    private userService: UserService,
    private translate: TranslateService
  ) {
    super(modalService, translate);

    this.registerForm = fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      passwordConfirm: ['', Validators.required],
      firstname: ['', [Validators.required, Validators.pattern(this.namePattern)]],
      lastname: ['', [Validators.required, Validators.pattern(this.namePattern)]],
      country: ['', [Validators.required, Validators.min(0), Validators.max(279)]],
    },
    {
      validator: this.passwordMatchValidator
    });
  }

  ngOnInit() {
    var t = this;
    if(t.authService.isLoggedIn){
      t.router.navigateByUrl('/my-workspaces');
    }
  }

  get email() { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }
  get passwordConfirm() { return this.registerForm.get('passwordConfirm'); }
  get firstname() { return this.registerForm.get('firstname'); }
  get lastname() { return this.registerForm.get('lastname'); }
  get country() { return this.registerForm.get('country');}

  passwordMatchValidator(g: FormGroup) {
    var password = g.get('password');
    if (!!password && password.dirty && !password.untouched) {
      var isValidPassword = ValidationUtils.validatePassword(password.value);
      if (!isValidPassword.isValid) {
        let obj: ValidationErrors = {};
        obj[isValidPassword.errorText] = true;
        password.setErrors(obj);
      }
    }

    var passwordConfirm = g.get('passwordConfirm');
    if (!!password && !!passwordConfirm && passwordConfirm.dirty && !passwordConfirm.untouched) {
      if (!passwordConfirm.value) {
        return;
      }

      var isValidPasswordConfirm = ValidationUtils.validatePasswordConfirmation(password.value, passwordConfirm.value);
      if (!isValidPasswordConfirm.isValid) {
        let obj: ValidationErrors = {};
        obj[isValidPasswordConfirm.errorText] = true;
        passwordConfirm.setErrors(obj);
      }
    }
  }

  public onSelectedCountry(event:any){
    this.selectedCountry = event.value;
    this.country?.setValue(this.selectedCountry);
  }

  public async register() {
    var t = this;
    if (t.registerForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.registerForm)
      return;
    }

    t.setLoading(true);

    var createUserReq: CreateUserPostRequest = {
      firstName: t.firstname?.value,
      lastName: t.lastname?.value,
      email: t.email?.value,
      password: t.password?.value,
      country: t.selectedCountry,
    };

    await t.publicService.register(createUserReq)
    .then((resp: any) => {
      var authModel : AuthenticatePostRequest = { email: t.email?.value, password: t.password?.value };
      t.authService.SignIn(authModel)
        .then(() => {
          t.userService.init();
          t.setLoading(false);
          t.showSuccess("Registration completed successfully", "Success");
          t.router.navigate(['/my-workspaces']);
        }).catch((ex) => {
          t.setLoading(false);
          t.showResponseError(ex);
        });
    })
    .catch((e) => {
      t.showResponseError(e);
    })
    .finally(() => {
      t.setLoading(false);
    });
  }
}
