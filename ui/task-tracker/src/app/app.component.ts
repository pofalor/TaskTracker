import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LoaderComponent } from './shared/components/loader/loader.component';
import { TranslateModule } from '@ngx-translate/core';
import {TranslateService} from "@ngx-translate/core";
import { AuthService } from './shared/services/auth.service';
import { UserService } from './shared/services/user.service';
import { EventService } from './shared/services/event.service';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, LoaderComponent, TranslateModule],
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
    private eventService: EventService) {
    this.translate.addLangs(['ru', 'en']);
    this.translate.setDefaultLang('en');
    this.translate.use('en');
    var t = this;
    t.router.events.subscribe((event) => {
      let intervals: Promise<any>[] = [];
      if (t.authService.isLoggedIn) {
        if (!t.userService.get() && !t.requestSent) {
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
