import { Component } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../../shared/services/auth.service';
import { BaseComponent } from '../../shared/base/base.component';
import { ReactiveFormsModule } from '@angular/forms';
import { WorkSpaceModel } from '../../shared/model/workSpaceModel';
import { WorkspaceService } from '../../shared/services/workspace.service';
import { WorkSpaceType } from '../../shared/enums/work-space-type';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-my-workspaces',
    imports: [ReactiveFormsModule, CommonModule],
    templateUrl: './my-workspaces.component.html',
    styleUrl: './my-workspaces.component.scss'
})
export class MyWorkspacesComponent extends BaseComponent {

allMyWorkSpaces: WorkSpaceModel[] = [];
WorkSpaceType = WorkSpaceType;

constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private workSpaceService: WorkspaceService
  ) {
    super(modalService);
  }

  async ngOnInit() {
    var t = this;
    await t.getMyWorkspaces();
  }

  public async getMyWorkspaces() {
    var t = this;

    t.setLoading(true);

    await t.workSpaceService.getMyWorkspaces()
    .then((resp: any) => {
      t.allMyWorkSpaces = resp.data;
    })
    .catch((e) => {
      t.showResponseError(e);
    })
    .finally(() => {
      t.setLoading(false);
    });
  }

  public async createWorkspace(){
    var t = this;
    t.setLoading(true);
    t.setLoading(false);
  }
}
