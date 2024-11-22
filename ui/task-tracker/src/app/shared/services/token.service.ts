import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ApiService } from  './api.service';
import { AuthenticatePostRequest } from '../model/postRequests/authenticatePostRequest';
import { AuthorizationModel } from '../model/authorizationModel';
const tokenUrl = environment.tokenUrl;

@Injectable({
  providedIn: 'root'
})
export class TokenService {

    constructor(private api: ApiService) { }

    public createToken(postRequest: AuthenticatePostRequest) {
        return this.api.postAnonym<AuthorizationModel>(tokenUrl, postRequest);
    }
}