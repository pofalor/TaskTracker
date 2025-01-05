import { Component } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../../shared/services/auth.service';
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

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private workSpaceService: WorkspaceService,
    private translate: TranslateService
  ) {
    super(modalService, translate);
  }

  async ngOnInit() {
    var t = this;
    await t.getMyWorkspaces();
  }

  public async getMyWorkspaces(needLoader: boolean = true) {
    var t = this;
    if(needLoader)
      t.setLoading(true);

    await t.workSpaceService.getMyWorkspaces()
      .then((resp: any) => {
        t.allMyWorkSpaces = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if(needLoader)
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

    t.modalRef.result.then(async (result) => {
      if (result) {
        await t.createOrEditWorkspace(result);
      }
    });
  }

  public async createOrEditWorkspace(createWorkspacePostRequest: CreateOrEditWorkSpacePostRequest){
    var t = this;
    t.setLoading(true);
    await t.workSpaceService.createOrEdit(createWorkspacePostRequest)
      .then(async (resp: any) => {
        if(!!resp && !!resp.data){
          await t.getMyWorkspaces(false);
          t.showSuccess("Workspace sucessfully created", "Success");
        }
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        t.setLoading(false);
      });
  }

  viewProjects(){

  }

  editWorkSpace(workSpaceId: number){

  }
}