import { Injectable, OnInit, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { TokenService } from "./token.service";
import { AuthData } from '../interfaces/auth-data'
import { ApiService } from './api.service';
import { firstValueFrom, map } from 'rxjs';
import { AuthenticatePostRequest } from '../model/postRequests/authenticatePostRequest';
import { UserService } from './user.service';


@Injectable({
  providedIn: 'root'
})
export class AuthService implements OnInit {

  private static _authData: any;

  constructor(
    private tokenService: TokenService,
    public router: Router,
    public ngZone: NgZone,
    private api: ApiService,
    // public eventService: EventService,
    // private cryptoUserService: CryptoUserService,
    // private statsService: StatsService,
    private userService: UserService
  ) {

  }

  ngOnInit(): void { }

  get authData() {
    var authVal = typeof window !== 'undefined' ? localStorage?.getItem("auth") : null;
    if (!AuthService._authData && !!authVal) {
      AuthService._authData = JSON.parse(authVal);
    }

    let authData = !!authVal ? JSON.parse(authVal) as AuthData : null;
    if (!!AuthService._authData && (!authData || AuthService._authData.accessToken != authData.accessToken)) {
      this.SignOut();
    }
    return AuthService._authData;
  }

  set authData(newAuth: AuthData | null) {
    var t = this;
    if(!!localStorage){
      if (!newAuth) {
        localStorage.removeItem("auth");
      } else {
        localStorage.setItem("auth", JSON.stringify(newAuth));
      }
    }
    AuthService._authData = newAuth;
  }

  //sign in function
  async SignIn(credentials: AuthenticatePostRequest) {
    var result = this.tokenService.createToken(credentials);
    var response = await firstValueFrom(result);
    if(!!response.errors && response.errors.some(x=> x)){
      var error = response.errors[0];
      throw new Error(error.message);
    }
    if(!response.data?.token){
      throw new Error("Received empty token.");
    }
    const tokenObj: AuthData = {
      accessToken: response.data?.token ?? "",
    };
    localStorage.setItem("accessToken", tokenObj.accessToken);
    this.authData = tokenObj;
  }

  // Sign out
  SignOut() {
    window.stop();
    this.authData = null;
    //TODO: сделать clear сервисов
    // this.cryptoUserService.clear();
    this.userService.clear();
    // this.statsService.clear();
    // this.eventService.logout();
    localStorage.clear(); //сбрасывает язык в localStorage (по дефолту 'en')
    this.router.navigate(['/login']);
  }

  get isLoggedIn(): boolean {
    return !!this.authData;
  }

}