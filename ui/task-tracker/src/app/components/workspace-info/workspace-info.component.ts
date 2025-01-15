import { Component } from '@angular/core';
import { BaseComponent } from '../../shared/base/base.component';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../shared/services/onlyFrontServices/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ProjectService } from '../../shared/services/project.service';
import { ProjectModel } from '../../shared/model/projectModel';
import { UserModel } from '../../shared/model/userModel';
import { UserService } from '../../shared/services/user.service';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { LangPipe } from '../../shared/pipes/lang.pipe';
import { WorkspaceService } from '../../shared/services/workspace.service';
import { UserWspStatusChangeModel } from '../../shared/model/userWspStatusChangeModel';
import { CreateWspInvitationModalComponent } from '../../shared/components/modals/create-wsp-invitation-modal/create-wsp-invitation.modal.component';
import { WorkSpaceModel } from '../../shared/model/workSpaceModel';

@Component({
  selector: 'app-workspace-info',
  imports: [ReactiveFormsModule, CommonModule, LangPipe],
  templateUrl: './workspace-info.component.html',
  styleUrl: './workspace-info.component.scss'
})
export class WorkspaceInfoComponent extends BaseComponent {
  public workspaceId!: string;
  allProjects: ProjectModel[] = [];
  public getUser: () => UserModel;
  public isWorkspaceOwner: boolean = false;
  public invitesMeCreated: UserWspStatusChangeModel[] = [];
  modalRef!: NgbModalRef;
  workspace: WorkSpaceModel = new WorkSpaceModel();

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private projectService: ProjectService,
    private translate: TranslateService,
    private activateRoute: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private workspaceService: WorkspaceService
  ) {
    super(modalService, translate);

    this.getUser = () => {
      var user = this.userService.get() ?? new UserModel();
      return user;
    }
  }

  async ngOnInit() {
    var t = this;
    var wspIdStr = t.activateRoute.snapshot.paramMap.get('workspaceId');
    if (!wspIdStr || Number.isNaN(wspIdStr)) {
      t.router.navigate(['/my-workspaces']);
      return;
    }
    t.workspaceId = wspIdStr;
    debugger
    var workspaceName = t.activateRoute.snapshot.queryParamMap.get('name');
    t.workspace.name = workspaceName ?? "";

    t.setLoading(true);
    Promise.all([
      t.getWorkspaceProjects(false),
      t.isUserWorkspaceOwner(false),
      t.getUserCreatedInvites(false)
    ])
      .then(() => {
        t.setLoading(false);
      });
  }

  public async getWorkspaceProjects(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.projectService.getWorkspaceProjects(+t.workspaceId)
      .then((resp: any) => {
        t.allProjects = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public async isUserWorkspaceOwner(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.workspaceService.isUserWorkspaceOwner(+t.workspaceId)
      .then((resp: any) => {
        t.isWorkspaceOwner = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public async getUserCreatedInvites(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.workspaceService.getUserCreatedInvites(+t.workspaceId)
      .then((resp: any) => {
        t.invitesMeCreated = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public createProject() {

  }

  public createInvite() {
    var t = this;

    t.modalRef = t.modalService.open(CreateWspInvitationModalComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.workspaces = t.workspace;
  }
}
