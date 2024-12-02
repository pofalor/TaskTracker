import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { LoaderComponent } from './shared/components/loader/loader.component';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, LoaderComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'task-tracker';
}
