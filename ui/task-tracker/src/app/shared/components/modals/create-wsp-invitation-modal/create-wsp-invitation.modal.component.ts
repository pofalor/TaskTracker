import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../base/base.component';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { NgbModal, NgbActiveModal, NgbAlertModule, NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { UserService } from '../../../services/user.service';
import { WorkspaceService } from '../../../services/workspace.service';
import { WorkSpaceModel } from '../../../model/workSpaceModel';
import { WorkSpaceType } from '../../../enums/work-space-type';
import { CreateWspInvitePostRequest } from '../../../model/postRequests/сreateWspInvitePostRequest';
import { SearchUserForInvitePR } from '../../../model/postRequests/searchUserForInvitePR';
import { UserModel } from '../../../model/userModel';
import { from, Observable, of, scheduled } from 'rxjs';
import { IResponse } from '../../../interfaces/response';
import { CommonModule } from '@angular/common';
import { LangPipe } from '../../../pipes/lang.pipe';
import { NgSelectComponent, NgOptionComponent } from '@ng-select/ng-select';
import { DatepickerUtils } from '../../../utils/ngbDatepickerUtils';
import { UserWorkSpaceStatus } from '../../../enums/user-workspace-status';
import { DateUtils } from '../../../utils/dateUtils';

@Component({
  selector: 'app-create-wsp-invitation-modal',
  imports: [CommonModule, LangPipe, ReactiveFormsModule, NgSelectComponent, NgOptionComponent, FormsModule,
    NgbAlertModule, NgbDatepickerModule],
  templateUrl: './create-wsp-invitation.modal.component.html',
  styleUrl: './create-wsp-invitation.modal.component.scss'
})
export class CreateWspInvitationModalComponent extends BaseComponent {
  @Input() invitationId: number | undefined;
  @Input() workspaces: WorkSpaceModel[] | undefined;
  @Input() workspace: WorkSpaceModel | undefined;
  @Input() useArray: boolean = false;
  public invitationForm!: FormGroup;
  newInvitation: CreateWspInvitePostRequest = new CreateWspInvitePostRequest();
  public allUsers: UserModel[] | undefined;
  public timer: any;
  searchText: string = "";

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private activeModal: NgbActiveModal,
    private workSpaceService: WorkspaceService,
    private translate: TranslateService,
    private userService: UserService,
  ) {
    super(modalService, translate);

  }

  get workspaceId() { return this.invitationForm.get('workspaceId'); }
  get userId() { return this.invitationForm.get('userId'); }

  ngOnInit() {
    var t = this;
    var workspaceId = null;
    if (t.useArray) {
      if (!!t.workspaces && t.workspaces.length > 0)
        workspaceId = t.workspaces[0].id;
    }
    else {
      workspaceId = t.workspace?.id ?? null;
    }

    t.invitationForm = t.fb.group({
      workspaceId: [workspaceId, [Validators.required]],
      userId: ['', [Validators.required, Validators.min(1)]]
    });
  }

  searchEvent(event: any) {
    var t = this;
    var timeOutTime = 1000;
    t.searchText = event.term;

    if (t.timer) {
      clearTimeout(t.timer);
      t.timer = setTimeout(t.searchUsersForInvite, timeOutTime, t);
    }
    else {
      t.timer = setTimeout(t.searchUsersForInvite, timeOutTime, t);
    }
  }

  public async searchUsersForInvite(t: any) {
    var wspId = t.workspaceId?.value;

    if (!wspId) {
      var userControl = t.userId;
      let obj: ValidationErrors = {};
      var error = t.translate.instant("Please select a workspace");
      obj[error] = true;
      userControl?.setErrors(obj);
      return;
    }

    var postRequest: SearchUserForInvitePR = {
      workSpaceId: wspId,
      inviterId: t.userService.get()?.id,
      search: t.searchText
    };

    await t.workSpaceService.searchUsersForInvite(postRequest)
      .then((resp: any) => {
        t.allUsers = resp.data;
      })
      .catch((e: any) => {
        t.showResponseError(e);
      })
  }

  back(result: boolean = false) {
    this.activeModal.close(result);
  }

  async createOrEditWspInvite() {
    var t = this;
    if (t.invitationForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.invitationForm)
      return;
    }
    t.setLoading(true);

    t.newInvitation.userId = t.userId?.value;
    t.newInvitation.inviterId = t.userService.get()?.id;
    t.newInvitation.date = DateUtils.getNowDateStr(true);
    t.newInvitation.newStatus = UserWorkSpaceStatus.Active;
    t.newInvitation.workSpaceId = t.workspaceId?.value;

    await t.workSpaceService.createWspInvite(t.newInvitation)
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          t.activeModal.close(t.newInvitation);
        }
      })
      .catch((e) => {
        t.setLoading(false);
        t.showResponseError(e);
      })
  }

}
