import { Component } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../../shared/services/onlyFrontServices/auth.service';
import { BaseComponent } from '../../shared/base/base.component';
import { ReactiveFormsModule } from '@angular/forms';
import { WorkspaceModel } from '../../shared/model/workspaceModel';
import { WorkspaceService } from '../../shared/services/workspace.service';
import { WorkspaceType } from '../../shared/enums/work-space-type';
import { CommonModule } from '@angular/common';
import { CreateWorkspaceModalComponent } from '../../shared/components/modals/create-workspace/create-workspace.modal.component';
import { LangPipe } from '../../shared/pipes/lang.pipe';
import { CreateOrEditWorkspacePostRequest } from '../../shared/model/postRequests/createOrEditWorkspacePostRequest';
import { TranslateService } from '@ngx-translate/core';
import { UserTeamRole } from '../../shared/enums/user-team-role';
import { DatepickerUtils } from '../../shared/utils/ngbDatepickerUtils';
import { Router } from '@angular/router';
import { UserWspStatusChangeModel } from '../../shared/model/userWspStatusChangeModel';
import { ModalInfoModel } from '../../shared/model/onlyFrontModels/modalInfo.model';
import { AcceptInvitePR } from '../../shared/model/postRequests/acceptInvitePR';
import { UserStatusChangeType } from '../../shared/enums/user-status-change-type';
import { UserService } from '../../shared/services/user.service';
import { UserModel } from '../../shared/model/userModel';
import { WorkspaceReviewStatus } from '../../shared/enums/workspace-review-status';
import { permissions } from '../../shared/constants/permissions';
import { ListCountry } from '../../shared/constants/country';

@Component({
  selector: 'app-my-workspaces',
  imports: [ReactiveFormsModule, CommonModule, LangPipe],
  templateUrl: './my-workspaces.component.html',
  styleUrl: './my-workspaces.component.scss'
})
export class MyWorkspacesComponent extends BaseComponent {
  allMyWorkspaces: WorkspaceModel[] = [];
  WorkspaceType = WorkspaceType;
  modalRef!: NgbModalRef;
  UserTeamRole = UserTeamRole;
  allMyInvitations: UserWspStatusChangeModel[] = [];
  UserStatusChangeType = UserStatusChangeType;
  runIntervals: any[] = [];
  public getUser: () => UserModel;
  WorkspaceReviewStatus = WorkspaceReviewStatus;
  workspacesForCheck: WorkspaceModel[] = [];
  permissions = permissions;
  ListCountry = ListCountry;

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private workspaceService: WorkspaceService,
    private translate: TranslateService,
    private router: Router,
    private userService: UserService,
  ) {
    super(modalService, translate);

    this.getUser = () => {
      var user = this.userService.get() ?? new UserModel();
      return user;
    }
  }

  async ngOnInit() {
    var t = this;
    t.setLoading(true);
    Promise.all([
      t.getMyWorkspaces(this, false),
      t.getMyInvitations(false),
      t.getWorkspacesForCheck(false)
    ])
      .then(() => {
        t.setIntervals();
        t.setLoading(false);
      });
  }

  ngOnDestroy() {
    this.terminatentervals(this);
  }

  private terminatentervals(t: any) {
    t.runIntervals.forEach((id: string | number | NodeJS.Timeout | undefined) => {
      clearInterval(id);
    });
    t.runIntervals = [];
  }

  private setIntervals() {
    let t = this;
    t.terminatentervals(t);
    t.runIntervals.push(setInterval(t.getMyWorkspaces, 5000, t, false));
  }

  public async getMyWorkspaces(t: any, needLoader: boolean = true) {
    if (needLoader)
      t.setLoading(true);

    await t.workspaceService.getMyWorkspaces()
      .then((resp: any) => {
        t.allMyWorkspaces = resp.data;
      })
      .catch((e: any) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public async getMyInvitations(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.workspaceService.getMyInvitations()
      .then((resp: any) => {
        t.allMyInvitations = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public async getWorkspacesForCheck(needLoader: boolean = true) {
    var t = this;
    if(!t.userService.verifyRole([permissions.Admin]))
      return;
        
    if (needLoader)
      t.setLoading(true);

    await t.workspaceService.getWorkspacesForCheck()
      .then((resp: any) => {
        t.workspacesForCheck = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public async changeReviewStatus(newStatus: WorkspaceReviewStatus, workspace: WorkspaceModel, needLoader: boolean = true) {
    var t = this;
    var action = newStatus == WorkspaceReviewStatus.Approved ? "approve" : "decline";
    t.showConfirm(`Are you sure you want to ${action} workspace?`, "Confirm the action", true, "Yes")
          .then(async (result) => {
            if (result) {
              t.setLoading(true);
              var request = new CreateOrEditWorkspacePostRequest();
              request.id = workspace.id;
              request.reviewStatus = newStatus;
              request.name = workspace.name;
              await t.workspaceService.changeWorkspaceReviewStatus(request)
                .then(async (resp: any) => {
                  if(resp){
                    var newAction = newStatus == WorkspaceReviewStatus.Approved ? "approved" : "declined";
                    t.showSuccess("Workspace successfully " + newAction);
                    await t.getWorkspacesForCheck(false);
                  }
                })
                .catch((e) => {
                  t.showResponseError(e);
                })
                .finally(() => {
                  t.setLoading(false);
                });
            }
          });
  }


  public async createWorkspace() {
    var t = this;

    t.modalRef = t.modalService.open(CreateWorkspaceModalComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.result.then(async (result) => t.processModalResult(result));
  }

  private async processModalResult(result: any) {
    if (result) {
      var t = this;
      await t.getMyWorkspaces(t, false);
      t.showSuccess("Workspace sucessfully " + (!!result.id ? "updated" : "created"), "Success");
      t.setLoading(false);
    }
  }


  viewWorkspace(workspace: WorkspaceModel) {
    this.router.navigate(['/workspace-info'],
      {
        queryParams: {
          "name": workspace.name,
          "workspaceId": workspace.id
        }
      });
  }

  editWorkspace(workspace: WorkspaceModel) {
    var t = this;

    t.modalRef = t.modalService.open(CreateWorkspaceModalComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.workspaceId = workspace.id;
    t.modalRef.componentInstance.workspaceName = workspace.name;
    t.modalRef.componentInstance.workspaceType = workspace.workspaceType;
    t.modalRef.componentInstance.workspaceCountry = workspace.country;
    t.modalRef.componentInstance.workspaceRegisterDate = DatepickerUtils.dateFromStr(workspace.registrationDate?.toString());
    t.modalRef.componentInstance.workspaceAddress = workspace.address;
    t.modalRef.componentInstance.workspaceINN = workspace.inn;

    t.modalRef.result.then(async (result) => t.processModalResult(result));
  }

  setRequestStatus(accept: boolean, inviteId: number) {
    var t = this;
    var modalInfo = new ModalInfoModel();
    var result = accept ? "accept" : "decline";
    modalInfo.title = `Are you sure you want to ${result} the request to join the workspace?`;
    modalInfo.showDescription = false;
    modalInfo.buttonConfirm = "Yes";
    modalInfo.buttonDecline = "No";
    t.showModal(modalInfo).then(async (result) => {
      if (result)
        await t.acceptInvite(accept, inviteId);
    });
  }

  async acceptInvite(accept: boolean, inviteId: number) {
    var t = this;
    t.setLoading(true);
    var postRequest: AcceptInvitePR = {
      id: inviteId,
      requestStatus: accept ? UserStatusChangeType.UserConfirmed : UserStatusChangeType.UserDeclined,
      userId: t.userService.get()?.id
    };

    await t.workspaceService.acceptInvitationRequest(postRequest)
      .then(async (resp: any) => {
        if (resp) {
          await t.getMyWorkspaces(t, false);
          await t.getMyInvitations(false);
          var result = accept ? "accepted" : "declined";
          t.showSuccess(`Request successfully ${result}`, "Success");
        }
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        t.setLoading(false);
      });
  }

  needShowWorkspace(workspace: WorkspaceModel) {
    var t = this;
    if (!!workspace.reviewStatus || workspace.workspaceType == WorkspaceType.Company) {
      //Пространство на проверке надо показывать только овнеру, остальным скрывать
      return !!workspace.reviewStatus && (workspace.reviewStatus != WorkspaceReviewStatus.OnReview || workspace.directorUserId == t.getUser().id);
    }
    else {
      return true;
    }
  }

  getCountryByCode(countryCode: number | undefined){
    var t = this;
    return t.ListCountry.find(x=> x.value == countryCode)?.name;
  }
}