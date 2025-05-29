import { Injectable } from '@angular/core';
import { IResponse } from '../interfaces/response';
import { lastValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { TimeTrackingModel } from '../model/timeTrackingModel';
import { TimeTrackPR } from '../model/postRequests/timeTrackPR';
import { AutoTimeTrackPR } from '../model/postRequests/autoTimeTrackPR';

const serviceApiUrl = 'api/autotrack/';

@Injectable({
  providedIn: 'root'
})
export class AutoTimeTrackService {

  constructor(private api: ApiService) { }

  public async getActiveAutoTrack(projectId: number, workspaceId: number): Promise<IResponse<TimeTrackingModel | null>> {
    var res = this.api.get<TimeTrackingModel | null>(serviceApiUrl + `getActiveAutoTrack?projectId=${projectId}&workspaceId=${workspaceId}`);
    return await lastValueFrom(res);
  } 
  
  public async startTracking(request: AutoTimeTrackPR): Promise<IResponse<TimeTrackingModel>> {
    var res = this.api.post<TimeTrackingModel>(serviceApiUrl + "startTracking", request);
    return await lastValueFrom(res);
  } 

  public async stopTracking(request: AutoTimeTrackPR): Promise<IResponse<TimeTrackingModel>> {
    var res = this.api.post<TimeTrackingModel>(serviceApiUrl + "stopTracking", request);
    return await lastValueFrom(res);
  } 
}
