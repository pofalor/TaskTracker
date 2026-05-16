import { ApplicationRef, Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LoaderComponent } from './shared/components/loader/loader.component';
import { TranslateModule } from '@ngx-translate/core';
import {TranslateService} from "@ngx-translate/core";
import { AuthService } from './shared/services/onlyFrontServices/auth.service';
import { UserService } from './shared/services/user.service';
import { EventService } from './shared/services/onlyFrontServices/event.service';
import { HeaderComponent } from './shared/components/header/header.component';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, LoaderComponent, TranslateModule, HeaderComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent {
  requestSent: boolean = false;

  constructor(
    private translate: TranslateService,
    private router: Router,
    private authService: AuthService,
    private userService: UserService,
    private eventService: EventService,
    private applicationRef: ApplicationRef) {
    this.translate.addLangs(['ru', 'en']);
    this.translate.setDefaultLang('en');
    const hasBrowserStorage = typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
    const defaultLang = hasBrowserStorage ? window.localStorage.getItem('localization') ?? 'en' : 'en';
    this.translate.use(defaultLang);
    if (typeof document !== 'undefined') {
      document.documentElement.lang = defaultLang;
    }
    var t = this;
    t.router.events.subscribe((event) => {
      let intervals: Promise<any>[] = [];
      if (t.authService.isLoggedIn && t.applicationRef.isStable) {
        if (!t.userService.get() && !t.requestSent) {
          //костыль, чтобы не летела куча запросов на бек
          t.requestSent = true;
          intervals.push(t.userService.init());
          t.eventService.addFuncToArrayOfIntervals(
            t.userService.refresh,
            1000 * 60 * 5
          );
        }

        Promise.all(intervals).then(
          (value) => {},
          (reason) => {
            this.authService.SignOut();
          }
        );
      }
    });
  }

  title = 'task-tracker';
}
