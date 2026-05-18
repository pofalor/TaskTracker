import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/onlyFrontServices/auth.service';
import { UserService } from '../services/user.service';
import { LoaderComponent } from '../components/loader/loader.component';

@Injectable({
  providedIn: 'root'
})
export class UserGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router, private userService: UserService) { }
  
  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): MaybeAsync<GuardResult> {
    var t = this;
    if (!t.authService.isLoggedIn) {
      t.router.navigate(['/login']);
      return false;
    }
    else {
      var accessRoles = route.data['roles'];

      if (!accessRoles) return true;
      if (!t.userService.getRole() || t.userService.getRole().length == 0) {
        const promise = new Promise<boolean>((resolve, reject) => {
          LoaderComponent.setLoading(true);
          t.userService.refresh()
            .then((resp) => {
              resolve(t.verifyRole(accessRoles));
            })
            .catch((error) => {
              resolve(t.goAway());
            })
            .finally(() => {
              LoaderComponent.setLoading(false);
            });
        });
        return promise;
      }
      else return t.verifyRole(accessRoles);
    }
  }

  verifyRole(accessRoles: string[]): boolean {
    var t = this;
    if (!accessRoles) {
      return t.goAway();
    }
    var resp = false;
    var userRoles = t.userService.getRole();
    accessRoles.forEach(role => {
      if (userRoles.indexOf(role) != -1) {
        resp = true;
      }
    });

    return resp ? resp : t.goAway();
  }

  goAway(): boolean {
    this.router.navigateByUrl('my-workspaces');
    return false;
  }

}
