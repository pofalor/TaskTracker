import { Injectable, OnInit, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { TokenService, Credentials } from "./token.service";
import { tap } from 'rxjs/internal/operators/tap';
import { AuthData } from '../interfaces/auth-data'
import { ApiService } from './api.service';


@Injectable({
  providedIn: 'root'
})
export class AuthService implements OnInit {

  private static _authData: any;
  public userData: any;
  public showLoader: boolean = false;

  constructor(
    private tokenService: TokenService,
    public router: Router,
    public ngZone: NgZone,
    private api: ApiService,
    public eventService: EventService,
    private cryptoUserService: CryptoUserService,
    private statsService: StatsService,
    private userService: UserService
  ) {

  }

  ngOnInit(): void { }

  get authData() {
    if (!AuthService._authData && localStorage.getItem("auth")) {
      AuthService._authData = JSON.parse(localStorage.getItem("auth"));
    }

    let cookAuth = !!localStorage.getItem("auth") ? JSON.parse(localStorage.getItem("auth")) as AuthData : null;
    if (!!AuthService._authData && (!cookAuth || AuthService._authData.refreshToken != cookAuth.refreshToken ||
      AuthService._authData.accessToken != cookAuth.accessToken)) {

      this.SignOut();
      // return;
    }
    return AuthService._authData;
  }

  set authData(newAuth: AuthData) {

    var t = this;
    if (!newAuth) {
      localStorage.removeItem("auth");
    } else {
      // t.cookieService.set("auth", JSON.stringify(newAuth), newAuth["expiresAt"], "/",
      // "",false, "Lax");
      localStorage.setItem("auth", JSON.stringify(newAuth));
    }
    AuthService._authData = newAuth;
  }

  //sign in function
  SignIn(credentials: Credentials) {
    return this.tokenService
      .createToken(credentials)
      .pipe(tap((data) => {
        //const expiresAt = moment().add(data["expires_in"], "s").toDate();
        const tokenObj: AuthData = {
          accessToken: data["access_token"],
          refreshToken: data["refresh_token"],
          //expiresAt
        };
        localStorage.setItem("accessToken", tokenObj.accessToken);
        this.authData = tokenObj;
        //this.initRefresh();
      })).toPromise();
    }

  // Sign out
  SignOut() {
    this.router.routeReuseStrategy.shouldReuseRoute = function () {
      return false;
    };

    window.stop();
    this.showLoader = false;
    AuthService._authData = undefined;
    this.cryptoUserService.clear();
    this.userService.clear();
    this.statsService.clear();
    this.eventService.logout();
    //localStorage.clear(); //сбрасывает язык в localStorage (по дефолту 'en')
    this.router.navigate(['/auth/login']);
  }

  get isLoggedIn(): boolean {
    return !!this.authData;
  }

}