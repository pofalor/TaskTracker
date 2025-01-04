import { EventEmitter, Injectable, Output } from '@angular/core';
import { UserService } from './user.service';

@Injectable({
  providedIn: 'root'
})
export class EventService {

  private arrIntervals: any = [];
  private arrForLogOut: any = [];

  @Output() MainProductEvent = new EventEmitter<boolean>();

  constructor(
    private userService: UserService
  ) {
  }

  public addFuncToArrayOfIntervals(func: any, interval: number) {
    this.arrIntervals.push(setInterval(func, interval))
  }

  public addFuncToArrayForLogout(func: any) {
    this.arrForLogOut.push(func);
  }

  public logout() {
    this.userService.clear();
    this.clearIntervals();
    this.clearModels();
    this.arrIntervals = [];
    this.arrForLogOut = [];
  }
  public clearIntervals() {
    this.arrIntervals.forEach((element: string | number | NodeJS.Timeout | undefined) => {
      clearInterval(element);
    });
  }

  public clearModels() {
    this.arrForLogOut.forEach((element: { clear: () => void; }) => {
      try{
        element.clear();
      }
      catch(ex)
      {
        
      }
    });
  }
}
