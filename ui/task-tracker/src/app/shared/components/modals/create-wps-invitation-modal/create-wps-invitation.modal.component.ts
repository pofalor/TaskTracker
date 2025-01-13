import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../base/base.component';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NgbModal, NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { UserService } from '../../../services/user.service';
import { WorkspaceService } from '../../../services/workspace.service';
import { WorkSpaceModel } from '../../../model/workSpaceModel';
import { WorkSpaceType } from '../../../enums/work-space-type';

@Component({
  selector: 'app-create-wps-invitation-modal',
  imports: [],
  templateUrl: './create-wps-invitation-modal.component.html',
  styleUrl: './create-wps-invitation-modal.component.scss'
})
export class CreateWpsInvitationModalComponent extends BaseComponent {
  @Input() invitationId: number | undefined;
  @Input() workspaces: WorkSpaceModel[] | undefined;
  @Input() workspace: WorkSpaceModel | undefined;
  public invitationForm!: FormGroup;

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
    this.invitationForm = t.fb.group({
      workspace: [t.workspace, [Validators.required, Validators.pattern(t.namePattern), Validators.maxLength(20)]],
      // country: [{ value: t.workSpaceCountry, disabled: true }, [Validators.required, Validators.min(0), Validators.max(279)]],
      // registrationDate: [{ value: t.workSpaceRegisterDate, disabled: true }, [Validators.required, t.dateValidator.bind(t)]],
      // address: [{ value: t.workSpaceAddress, disabled: true }, [Validators.required, Validators.maxLength(300)]],
      // TIN: [{ value: t.workSpaceINN, disabled: true }, [Validators.required, Validators.maxLength(50)]],
    });
  }
}
