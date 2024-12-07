import { Injectable } from '@angular/core';
import { UserModel } from '../model/userModel';
import { ApiService } from './api.service';
import { lastValueFrom } from 'rxjs';

const userServiceApiUrl = 'api/user/';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  public user: UserModel | undefined;

  constructor(
    private api: ApiService
  ) {

  }

  public clear() {
    this.user = undefined;
  }

  public async init(): Promise<UserModel | undefined> {
    var result = this.get();
    if(!result){
      result = await this.refresh();
    }
    return result;
  }

  public isAuthentificated(): boolean {
    var at = localStorage?.getItem("accessToken");
    return at != null && at.length > 0;
  }

  public async refresh(): Promise<UserModel | undefined> {
    var promise = this.api.get<UserModel>(userServiceApiUrl + 'getUser');
    var resp = await lastValueFrom(promise);
    this.user = resp.data;
    if (this.isAuthentificated()) return this.user;
    else return new UserModel;
  }

  public get() {
    return this.user;
  }

  //возвращает роли текущего пользователя
  public getRole(): string[] {
    if (!!this.user)
      return this.user.roles;
    else
      return [];
  }

  //поверка на присутствие роли у пользователя
  //возвращает true, если у него есть хотя бы одна из указанных в массиве accessRoles
  public verifyRole(accessRoles: string[]): boolean {
    var t = this;
    if (!accessRoles) {
      return false;
    }
    var resp = false;
    var userRoles = t.getRole();
    if (userRoles != null) {
      accessRoles.forEach(role => {
        if (userRoles.indexOf(role) != -1) {
          resp = true;
        }
      });
    }
    return resp;
  }
}
