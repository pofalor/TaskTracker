/// <reference types="@angular/localize" />

import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
/// <reference path="app/shared/utils/global.d.ts" />
//...
import './app/shared/utils/string.extensions';

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
