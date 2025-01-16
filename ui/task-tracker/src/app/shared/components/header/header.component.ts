import { Component } from '@angular/core';
import { AuthService } from '../../services/onlyFrontServices/auth.service';
import { CommonModule } from '@angular/common';
import {Location} from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  constructor(
    private authService: AuthService,
    private location: Location,
    private router: Router
  ) {}

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
