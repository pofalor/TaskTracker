import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { LoaderComponent } from './shared/components/loader/loader.component';
import { TranslateModule } from '@ngx-translate/core';
import {TranslateService} from "@ngx-translate/core";

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, LoaderComponent, TranslateModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent {
  constructor(private translate: TranslateService) {
    this.translate.addLangs(['ru', 'en']);
    this.translate.setDefaultLang('en');
    this.translate.use('en');
  }

  title = 'task-tracker';
}
