import { Injectable } from '@angular/core';
import { IssueModel } from '../model/issueModel';
import { IssueFilter } from '../model/filters/issueFilter';
import { IResponse } from '../interfaces/response';
import { lastValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { CreateOrEditIssuePR } from '../model/postRequests/createOrEditIssuePR';
import { TimeTrackPR } from '../model/postRequests/timeTrackPR';
import { UserModel } from '../model/userModel';
import { TimeTrackingModel } from '../model/timeTrackingModel';

const serviceApiUrl = 'api/issue/';

@Injectable({
  providedIn: 'root'
})
export class IssueService {

  constructor(private api: ApiService) { }

  public async getProjectIssues(filter: IssueFilter): Promise<IResponse<IssueModel[]>> {
    var res = this.api.post<IssueModel[]>(serviceApiUrl + `getProjectIssues`, filter);
    return await lastValueFrom(res);
  }

  public async createOrEdit(postRequest: CreateOrEditIssuePR): Promise<IResponse<boolean>> {
    var res = this.api.post<boolean>(serviceApiUrl + 'add', postRequest);
    return await lastValueFrom(res);
  }

  public async trackTime(postRequest: TimeTrackPR): Promise<IResponse<boolean>> {
    var res = this.api.post<boolean>(serviceApiUrl + 'trackTime', postRequest);
    return await lastValueFrom(res);
  }
}
