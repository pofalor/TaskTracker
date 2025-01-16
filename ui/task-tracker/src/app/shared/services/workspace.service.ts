import { Injectable } from '@angular/core';
import { CreateOrEditWorkSpacePostRequest } from '../model/postRequests/createOrEditWorkSpacePostRequest';
import { IResponse } from '../interfaces/response';
import { ApiService } from './api.service';
import { lastValueFrom, Observable } from 'rxjs';
import { WorkSpaceModel } from '../model/workSpaceModel';
import { UserWspStatusChangeModel } from '../model/userWspStatusChangeModel';
import { CreateWspInvitePostRequest } from '../model/postRequests/сreateWspInvitePostRequest';
import { SearchUserForInvitePR } from '../model/postRequests/searchUserForInvitePR';
import { UserModel } from '../model/userModel';

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

  public async getMyInvitations(): Promise<IResponse<UserWspStatusChangeModel[]>> {
    var res = this.api.get<UserWspStatusChangeModel[]>(serviceApiUrl + 'getUserInvitations');
    return await lastValueFrom(res);
  }

  public async createWspInvite(postRequest: CreateWspInvitePostRequest): Promise<IResponse<boolean>> {
    var res = this.api.post<boolean>(serviceApiUrl + 'createWspInvite', postRequest);
    return await lastValueFrom(res);
  }

  public async searchUsersForInvite(postRequest: SearchUserForInvitePR): Promise<IResponse<UserModel[]>> {
    var res = this.api.post<UserModel[]>(serviceApiUrl + 'searchUsersForInvite', postRequest);
    return await lastValueFrom(res);
  }

  public async isUserWorkspaceOwner(workspaceId: number): Promise<IResponse<boolean>> {
    var res = this.api.get<boolean>(serviceApiUrl + `isUserWorkspaceOwner?workspaceId=${workspaceId}`);
    return await lastValueFrom(res);
  }

  public async getUserCreatedInvites(workspaceId: number): Promise<IResponse<UserWspStatusChangeModel[]>> {
    var res = this.api.get<UserWspStatusChangeModel[]>(serviceApiUrl + `getUserCreatedInvites?workspaceId=${workspaceId}`);
    return await lastValueFrom(res);
  }

}
