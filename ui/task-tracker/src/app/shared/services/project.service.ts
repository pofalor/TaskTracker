import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { ProjectModel } from '../model/projectModel';
import { IResponse } from '../interfaces/response';
import { lastValueFrom } from 'rxjs';

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
}
