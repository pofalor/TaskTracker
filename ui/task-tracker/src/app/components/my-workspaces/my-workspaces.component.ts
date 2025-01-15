import { Component } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../../shared/services/onlyFrontServices/auth.service';
import { BaseComponent } from '../../shared/base/base.component';
import { ReactiveFormsModule } from '@angular/forms';
import { WorkSpaceModel } from '../../shared/model/workSpaceModel';
import { WorkspaceService } from '../../shared/services/workspace.service';
import { WorkSpaceType } from '../../shared/enums/work-space-type';
import { CommonModule } from '@angular/common';
import { CreateWorkspaceModalComponent } from '../../shared/components/modals/create-workspace/create-workspace.modal.component';
import { LangPipe } from '../../shared/pipes/lang.pipe';
import { CreateOrEditWorkSpacePostRequest } from '../../shared/model/postRequests/createOrEditWorkSpacePostRequest';
import { TranslateService } from '@ngx-translate/core';
import { UserTeamRole } from '../../shared/enums/user-team-role';
import { DatepickerUtils } from '../../shared/utils/ngbDatepickerUtils';
import { Router } from '@angular/router';
import { UserWspStatusChangeModel } from '../../shared/model/userWspStatusChangeModel';

@Component({
  selector: 'app-my-workspaces',
  imports: [ReactiveFormsModule, CommonModule, LangPipe],
  templateUrl: './my-workspaces.component.html',
  styleUrl: './my-workspaces.component.scss'
})
export class MyWorkspacesComponent extends BaseComponent {
  allMyWorkSpaces: WorkSpaceModel[] = [];
  WorkSpaceType = WorkSpaceType;
  modalRef!: NgbModalRef;
  UserTeamRole = UserTeamRole;
  allMyInvitations: UserWspStatusChangeModel[] = [];

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private workSpaceService: WorkspaceService,
    private translate: TranslateService,
    private router: Router,
  ) {
    super(modalService, translate);
  }

  async ngOnInit() {
    var t = this;
    t.setLoading(true);
    Promise.all([
      t.getMyWorkspaces(false),
      t.getMyInvitations(false)
    ])
    .then(()=>{
      t.setLoading(false);
    });
  }

  public async getMyWorkspaces(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.workSpaceService.getMyWorkspaces()
      .then((resp: any) => {
        t.allMyWorkSpaces = resp.data;
      })
      .catch((e) => {
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

    await t.workSpaceService.getMyInvitations()
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
      await t.getMyWorkspaces(false);
      t.showSuccess("Workspace sucessfully " + (!!result.id ? "updated" : "created"), "Success");
      t.setLoading(false);
    }
  }


  viewWorkspace(workspace: WorkSpaceModel) {
    this.router.navigate(['/workspace-info/', workspace.id], 
      { 
        queryParams: {
          "name": workspace.name
        }
      });
  }

  editWorkSpace(workSpace: WorkSpaceModel) {
    var t = this;

    t.modalRef = t.modalService.open(CreateWorkspaceModalComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.workSpaceId = workSpace.id;
    t.modalRef.componentInstance.workSpaceName = workSpace.name;
    t.modalRef.componentInstance.workSpaceType = workSpace.workSpaceType;
    t.modalRef.componentInstance.workSpaceCountry = workSpace.country;
    t.modalRef.componentInstance.workSpaceRegisterDate = DatepickerUtils.dateFromStr(workSpace.registrationDate?.toString());
    t.modalRef.componentInstance.workSpaceAddress = workSpace.address;
    t.modalRef.componentInstance.workSpaceINN = workSpace.inn;

    t.modalRef.result.then(async (result) => t.processModalResult(result));
  }
}