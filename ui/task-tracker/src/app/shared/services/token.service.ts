import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { ApiService } from  './api.service';

export interface Credentials {
    email: string;
    password: string;
}
const tokenUrl = environment.tokenUrl;
const apiUrl = environment.apiUrl;

@Injectable({
  providedIn: 'root'
})
export class TokenService {

    private requestHeader = { headers:
      {
        'Content-Type': 'application/x-www-form-urlencoded',
        "Localization":"RU"
      }
};

    constructor(private http: HttpClient, private api: ApiService) { }

    public createToken(credentials: Credentials) {
        const requestData = {
            // ...oauthClientParams,
            grant_type: 'password',
            username: credentials.email,
            password: credentials.password
        };
        const requestBody = TokenService.encodeToFormdata(requestData);
        return this.api.postAnonym(tokenUrl, requestBody);
    }

    public refreshToken(refreshToken: string) {
        const requestData = {
            grant_type: 'refresh_token',
            refresh_token: refreshToken
        };
        const requestBody = TokenService.encodeToFormdata(requestData);
        return this.http.post(apiUrl + tokenUrl, requestBody, this.requestHeader);
    }

    private static encodeToFormdata(requestData : any) {
        return Object.keys(requestData).map((key) => {
            return encodeURIComponent(key) + '=' + encodeURIComponent(requestData[key]);
        }).join('&');
    }
}
