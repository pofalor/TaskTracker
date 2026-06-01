import { Component, OnDestroy, OnInit } from '@angular/core';
import { AuthService } from '../../services/onlyFrontServices/auth.service';
import { CommonModule } from '@angular/common';
import {Location} from '@angular/common';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, OnDestroy {
  public currentLang = 'en';
  public languages = [
    { code: 'en', label: 'EN' },
    { code: 'ru', label: 'RU' }
  ];
  private langChangeSubscription: Subscription | undefined;

  constructor(
    private authService: AuthService,
    private location: Location,
    private router: Router,
    private translate: TranslateService
  ) {
    this.syncCurrentLanguage();
  }

  ngOnInit() {
    this.langChangeSubscription = this.translate.onLangChange.subscribe((event) => {
      this.currentLang = event.lang;
    });
    this.syncCurrentLanguage();
  }

  ngOnDestroy() {
    this.langChangeSubscription?.unsubscribe();
  }

  changeLanguage(target: Event | null) {
    let lang = target ? (target.target as HTMLSelectElement).value : null;
    if (!lang || this.currentLang === lang) {
      return;
    }

    this.currentLang = lang;
    this.translate.use(lang);
    if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
      window.localStorage.setItem('localization', lang);
    }
    if (typeof document !== 'undefined') {
      document.documentElement.lang = lang;
    }
  }

  logout(){
    this.authService.SignOut();
  }

  isLoggedIn(){
    this.syncCurrentLanguage();
    return this.authService.isLoggedIn;
  }

  back(){
    var navId = window.history.state.navigationId;
    if(navId != 1)
      this.location.back();
    else 
      this.router.navigate(['/login']);
  }

  private syncCurrentLanguage() {
    const hasBrowserStorage = typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
    const storedLang = hasBrowserStorage ? window.localStorage.getItem('localization') : null;
    const lang = storedLang || this.translate.currentLang || 'en';
    this.currentLang = lang;
    if (this.translate.currentLang !== lang) {
      this.translate.use(lang);
      if (typeof document !== 'undefined') {
        document.documentElement.lang = lang;
      }
    }
  }
}
