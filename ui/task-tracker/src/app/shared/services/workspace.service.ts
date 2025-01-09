import { Injectable } from '@angular/core';
import { CreateOrEditWorkSpacePostRequest } from '../model/postRequests/createOrEditWorkSpacePostRequest';
import { IResponse } from '../interfaces/response';
import { ApiService } from './api.service';
import { lastValueFrom } from 'rxjs';
import { WorkSpaceModel } from '../model/workSpaceModel';

const serviceApiUrl = 'api/workspace/';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {

  constructor(private api: ApiService) { }

  public async getMyWorkspaces(): Promise<IResponse<WorkSpaceModel[]>> {
    var res = this.api.get<WorkSpaceModel[]>(serviceApiUrl + 'getMyWorkspaces');
    return await lastValueFrom(res);
  }

  public async createOrEdit(postRequest: CreateOrEditWorkSpacePostRequest): Promise<IResponse<boolean>> {
    var res = this.api.post<boolean>(serviceApiUrl + 'add', postRequest);
    return await lastValueFrom(res);
  }
}
