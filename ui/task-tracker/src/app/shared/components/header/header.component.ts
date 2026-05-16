import { Component } from '@angular/core';
import { AuthService } from '../../services/onlyFrontServices/auth.service';
import { CommonModule } from '@angular/common';
import {Location} from '@angular/common';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-header',
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  public currentLang = localStorage?.getItem('localization') ?? 'en';
  public languages = [
    { code: 'en', label: 'English' },
    { code: 'ru', label: 'Русский' }
  ];

  constructor(
    private authService: AuthService,
    private location: Location,
    private router: Router,
    private translate: TranslateService
  ) {}

  changeLanguage(target: Event | null) {
    let lang = target ? (target.target as HTMLSelectElement).value : null;
    if (!lang || this.currentLang === lang) {
      return;
    }

    this.currentLang = lang;
    this.translate.use(lang);
    localStorage?.setItem('localization', lang);
    if (typeof document !== 'undefined') {
      document.documentElement.lang = lang;
    }
  }

  logout(){
    this.authService.SignOut();
  }

  isLoggedIn(){
    return this.authService.isLoggedIn;
  }

  back(){
    var navId = window.history.state.navigationId;
    if(navId != 1)
      this.location.back();
    else 
      this.router.navigate(['/login']);
  }
}
