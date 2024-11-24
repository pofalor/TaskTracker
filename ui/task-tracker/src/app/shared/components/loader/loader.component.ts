import { Component } from '@angular/core';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [],
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.scss'
})
export class LoaderComponent {
  public static setLoading(isLoading: boolean) {
    var preloader = document.getElementById("preloader");
    if(!!preloader){
      if (isLoading) {
        preloader.style.display = 'flex';
      } else {
        preloader.style.display = 'none';
      }
    }
  };
}
