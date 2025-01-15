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
  public allUsersObservable!: Observable<UserModel[] | undefined>;
  public allUsers: UserModel[] | undefined;

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

    this.invitationForm = t.fb.group({
      workspaceId: [workspaceId, [Validators.required]],
      userId: ['', [Validators.required]],
      search: ['', [Validators.required]]
    });
  }

  get workspaceId() { return this.invitationForm.get('workspaceId'); }
  get userId() { return this.invitationForm.get('userId'); }
  get search() { return this.invitationForm.get('search'); }

  public async searchUsersForInvite() {
    var t = this;
    var wspId = t.workspaceId?.value;

    if (!!wspId) {
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
      search: t.search?.value
    };

    await t.workSpaceService.searchUsersForInvite(postRequest)
    .then((resp: any) => {
      t.allUsers = resp.data;
    })
    .catch((e) => {
      t.showResponseError(e);
    })

    t.allUsersObservable = of(t.allUsers);
  }

  back(result: boolean = false) {
    this.activeModal.close(result);
  }

  createOrEditWspInvite(){

  }

}
