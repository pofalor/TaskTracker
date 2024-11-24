import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { AuthenticatePostRequest } from '../model/postRequests/authenticatePostRequest';
import { IResponse } from '../interfaces/response';
import { lastValueFrom } from 'rxjs';

const accountApiUrl = 'api/Account/';

@Injectable({
  providedIn: 'root'
})
export class PublicService {

  constructor(private api: ApiService) { }

  public async register(registerPostRequest: AuthenticatePostRequest): Promise<IResponse<boolean>> {
    var res = this.api.postAnonym<boolean>(accountApiUrl + 'register', registerPostRequest);
    return await lastValueFrom(res);
  }
}