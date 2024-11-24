import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { BaseComponent } from '../../shared/base/base.component';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../shared/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent extends BaseComponent {
  public registerForm: FormGroup;

  constructor(
    public authService: AuthService,
    private fb: FormBuilder,
    private router: Router,
    private modalService: NgbModal,
  ) {
    super(modalService);

    this.registerForm = fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      passwordConfirm: ['', Validators.required],
      firstname: ['', [Validators.required, Validators.pattern(this.namePattern)]],
      lastname: ['', [Validators.required, Validators.pattern(this.namePattern)]],
      country: ['Russia', [Validators.required, Validators.maxLength(20), Validators.pattern(this.namePattern)]],
    });
  }

  // passwordMatchValidator(g: FormGroup) {
  //   var password = g.get('password');
  //   if(!!password && password.dirty && !password.untouched)
  //   {
  //     var isValidPassword = ValidationUtils.validatePassword(password.value);
  //     if(!isValidPassword.isValid)
  //     {
  //       let obj = {};
  //       obj[isValidPassword.errorText] = true;
  //       password.setErrors(obj);
  //     }
  //   }

  //   var passwordConfirm = g.get('passwordConfirm');
  //   if(passwordConfirm.dirty && !passwordConfirm.untouched)
  //   {
  //     if (!passwordConfirm.value) {
  //       return;
  //     }

  //     var isValidPasswordConfirm = ValidationUtils.validatePasswordConfirmation(password.value, passwordConfirm.value);
  //     if(!isValidPasswordConfirm.isValid)
  //     {
  //       let obj = {};
  //       obj[isValidPasswordConfirm.errorText] = true;
  //       passwordConfirm.setErrors(obj);
  //     }
  //   }

  public register() {
    var t = this;
    if (t.registerForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.registerForm)
      return;
    }

    t.setLoading(true);

    // t.publicService.register({
    //   contact: t.email.value,
    //   password: t.password.value,
    //   confirmPassword: t.passwordConfirm.value,
    //   captcha: (<any>window).recaptcha_code,
    //   refUser: t.refUser,
    //   localization: t.currentUserLang,
    //   lastName: t.lastname.value,
    //   firstName: t.firstname.value,
    //   companyName: t.companyName.value,
    //   registryNumber: t.registryNumber.value,
    //   registryDate: t.registryDate.value,
    //   registryCountry: t.registryCountry.value,
    //   mailingLanguage: t.selectedMailLang,
    //   legalAddress: t.legalAddress.value,
    //   TIN: t.TIN.value,
    //   nameOfRepresentative: t.nameOfRepresentative.value,
    //   userType: t.userType
    // }).then((resp: any) => {
    //   t.authService.SignIn({email: t.email.value,  password: t.password.value})
    //     .then(() => {
    //       t.showSuccess(t.translate.instant("Registration completed successfully") + ". " + t.translate.instant("Activation link has has been sent to your e-mail") + ": " + t.email.value, t.translate.instant("Success"));
    //       t.setLoading(false);
    //       t.router.navigate(['/auth/login']);
    //     }).catch((ex) => {
    //       t.setLoading(false);
    //       var test = ex;
    //     });
    // }).catch((e) => {
    //   t.showResponseError(e);
    //   t.setLoading(false);
    // });
    //t.setLoading(false);
  }
}
