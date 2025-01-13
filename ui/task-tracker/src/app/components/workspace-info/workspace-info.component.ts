import { Component } from '@angular/core';
import { BaseComponent } from '../../shared/base/base.component';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../shared/services/auth.service';
import { WorkspaceService } from '../../shared/services/workspace.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-workspace-info',
  imports: [],
  templateUrl: './workspace-info.component.html',
  styleUrl: './workspace-info.component.scss'
})
export class WorkspaceInfoComponent extends BaseComponent {
  public workspaceId: string | null = null;

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private workSpaceService: WorkspaceService,
    private translate: TranslateService,
    private activateRoute: ActivatedRoute,
  ) {
    super(modalService, translate);
  }

  async ngOnInit() {
    var t = this;
    t.workspaceId = this.activateRoute.snapshot.paramMap.get('workspaceId');
  }
}
