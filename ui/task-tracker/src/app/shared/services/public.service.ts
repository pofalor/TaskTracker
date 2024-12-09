import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { IResponse } from '../interfaces/response';
import { lastValueFrom } from 'rxjs';
import { CreateUserPostRequest } from '../model/postRequests/createUserPostRequest';

const authApiUrl = 'api/auth/';

@Injectable({
  providedIn: 'root'
})
export class PublicService {

  constructor(private api: ApiService) { }

  public async register(registerPostRequest: CreateUserPostRequest): Promise<IResponse<boolean>> {
    var res = this.api.postAnonym<boolean>(authApiUrl + 'register', registerPostRequest);
    return await lastValueFrom(res);
  }
}