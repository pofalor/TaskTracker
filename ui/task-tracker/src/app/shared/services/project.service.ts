import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { ProjectModel } from '../model/projectModel';
import { IResponse } from '../interfaces/response';
import { lastValueFrom } from 'rxjs';
import { CreateOrEditProjectPR } from '../model/postRequests/createOrEditProjectPR';
import { UserModel } from '../model/userModel';

const serviceApiUrl = 'api/project/';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {

  constructor(private api: ApiService) { }

  public async getWorkspaceProjects(workspaceId: number): Promise<IResponse<ProjectModel[]>> {
    var res = this.api.get<ProjectModel[]>(serviceApiUrl + `getWorkspaceProjects?workspaceId=${workspaceId}`);
    return await lastValueFrom(res);
  }

  public async getProjectMgrCandidates(workspaceId: number): Promise<IResponse<UserModel[]>> {
    var res = this.api.get<UserModel[]>(serviceApiUrl + `getProjectMgrCandidates?workspaceId=${workspaceId}`);
    return await lastValueFrom(res);
  }

  public async createOrEdit(postRequest: CreateOrEditProjectPR): Promise<IResponse<boolean>> {
    var res = this.api.post<boolean>(serviceApiUrl + 'add', postRequest);
    return await lastValueFrom(res);
  }
}
