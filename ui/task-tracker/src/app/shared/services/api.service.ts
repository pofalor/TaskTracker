import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Observable } from 'rxjs';
import { IResponse } from "../interfaces/response";
import { environment } from '../../../environments/environment';

const apiUrl = environment.apiUrl;

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  public getAnonym<T>(url: string): Observable<IResponse<T>> {
    return this.http.get<IResponse<T>>(apiUrl + url);
  }

  public get<T>(url: string): Observable<IResponse<T>> {
    return this.http.get<IResponse<T>>(apiUrl + url, { headers: this.getAuthorizationHeaders() });
  }

  public delete<T>(url: string): Observable<IResponse<T>> {
    return this.http.delete<IResponse<T>>(apiUrl + url, { headers: this.getAuthorizationHeaders() });
  }

  public postAnonym<T>(url: string, body: any): Observable<IResponse<T>> {
    return this.http.post<IResponse<T>>(apiUrl + url, body, { headers: this.getPublicHeaders() });
  }

  public post<T>(url: string, body: any): Observable<IResponse<T>> {
    return this.http.post<IResponse<T>>(apiUrl + url, body, { headers: this.getAuthorizationHeaders() });
  }

  public putAnonym<T>(url: string, body: any): Observable<IResponse<T>> {
    return this.http.put<IResponse<T>>(apiUrl + url, body);
  }

  public put<T>(url: string, body: any): Observable<IResponse<T>> {
    return this.http.put<IResponse<T>>(apiUrl + url, body, { headers: this.getAuthorizationHeaders() });
  }

  private getPublicHeaders() {
    return {
      "Content-Type": "application/json",
      "Localization": localStorage.getItem("localization") ?? "en",
      "Uuid": localStorage.getItem("Uuid") ?? "",
    }
  }

  private getAuthorizationHeaders(): any {
    let accessToken = localStorage.getItem("accessToken");
    if (accessToken) {

      return {
        "AuthorizationPolicy": `Bearer ${accessToken}`,
        "Content-Type": "application/json",
        "Localization": localStorage.getItem("localization") ?? "en",
      };
    }
    return null;
  }
}
