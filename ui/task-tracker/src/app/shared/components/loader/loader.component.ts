import { Component } from '@angular/core';

@Component({
    selector: 'app-loader',
    imports: [],
    templateUrl: './loader.component.html',
    styleUrl: './loader.component.scss'
})
export class LoaderComponent {
  private static activeLoaders = 1;

  public static setLoading(isLoading: boolean) {
    LoaderComponent.activeLoaders = isLoading
      ? LoaderComponent.activeLoaders + 1
      : Math.max(LoaderComponent.activeLoaders - 1, 0);

    if (typeof document === 'undefined') {
      return;
    }

    var preloader = document.getElementById("preloader");
    if(!!preloader){
      if (LoaderComponent.activeLoaders > 0) {
        preloader.style.display = 'flex';
      } else {
        preloader.style.display = 'none';
      }
    }
  };

  public static reset() {
    LoaderComponent.activeLoaders = 0;
    if (typeof document === 'undefined') {
      return;
    }

    var preloader = document.getElementById("preloader");
    if(!!preloader){
      preloader.style.display = 'none';
    }
  }
}
