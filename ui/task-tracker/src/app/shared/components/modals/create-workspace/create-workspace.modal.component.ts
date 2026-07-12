import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../base/base.component';
import { NgbActiveModal, NgbAlertModule, NgbDatepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { WorkspaceType } from '../../../enums/work-space-type';
import { CreateOrEditWorkspacePostRequest } from '../../../model/postRequests/createOrEditWorkSpacePostRequest';
import { LangPipe } from '../../../pipes/lang.pipe';
import { CommonModule } from '@angular/common';
import { ListCountry } from '../../../constants/country';
import { NgOptionComponent, NgSelectComponent } from '@ng-select/ng-select';
import { WorkspaceService } from '../../../services/workspace.service';
import { TranslateService } from '@ngx-translate/core';
import { UserModel } from '../../../model/userModel';
import { UserService } from '../../../services/user.service';
import { DatepickerUtils } from '../../../utils/ngbDatepickerUtils';
import { WorkspaceReviewStatus } from '../../../enums/workspace-review-status';


@Component({
  selector: 'app-create-workspace-modal',
  imports: [CommonModule, LangPipe, ReactiveFormsModule,
    NgSelectComponent, NgOptionComponent, NgbAlertModule, NgbDatepickerModule,],
  templateUrl: './create-workspace.modal.component.html',
  styleUrl: './create-workspace.modal.component.scss'
})
export class CreateWorkspaceModalComponent extends BaseComponent {
  @Input() workspaceId: number | undefined;
  @Input() workspaceName: string = '';
  @Input() workspaceType: WorkspaceType = WorkspaceType.Personal;
  @Input() workspaceCountry: number | undefined;
  @Input() workspaceRegisterDate: string | undefined = '';
  @Input() workspaceAddress: string | undefined = '';
  @Input() workspaceINN: string | undefined;
  public workspaceForm!: FormGroup;
  workspace: CreateOrEditWorkspacePostRequest = new CreateOrEditWorkspacePostRequest();
  WorkspaceType = WorkspaceType;
  public countryList: any[] = ListCountry;
  maxNameLength: number = 15;

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private activeModal: NgbActiveModal,
    private workspaceService: WorkspaceService,
    private translate: TranslateService,
    private userService: UserService,
  ) {
    super(modalService, translate);
  }

  get name() { return this.workspaceForm.get('name'); }
  get country() { return this.workspaceForm.get('country'); }
  get registrationDate() { return this.workspaceForm.get('registrationDate'); }
  get address() { return this.workspaceForm.get('address'); }
  get TIN() { return this.workspaceForm.get('TIN'); }

  ngOnInit() {
    var t = this;
    t.workspace.workspaceType = t.workspaceType;
    this.workspaceForm = t.fb.group({
      name: [t.workspaceName, [Validators.required, Validators.pattern(t.namePattern), Validators.maxLength(t.maxNameLength)]],
      country: [{ value: t.workspaceCountry, disabled: true }, [Validators.required, Validators.min(0), Validators.max(279)]],
      registrationDate: [{ value: t.workspaceRegisterDate, disabled: true }, [Validators.required, t.baseDateValidator.bind(t)]],
      address: [{ value: t.workspaceAddress, disabled: true }, [Validators.required, Validators.maxLength(300)]],
      TIN: [{ value: t.workspaceINN, disabled: true }, [Validators.required, Validators.maxLength(50)]],
    });

    if(t.workspace.workspaceType == WorkspaceType.Company)
      t.changeWorkspaceType(t.workspace.workspaceType);
  }

  changeWorkspaceType(workspaceType: WorkspaceType) {
    var t = this;
    t.workspace.workspaceType = workspaceType;
    switch (workspaceType) {
      case WorkspaceType.Company:
        t.country?.enable();
        t.registrationDate?.enable();
        t.address?.enable();
        t.TIN?.enable();
        break;
      case WorkspaceType.Personal:
        t.country?.disable();
        t.registrationDate?.disable();
        t.address?.disable();
        t.TIN?.disable();
        break;
    }
  }

  async createOrEditWorkspace() {
    var t = this;
    if (t.workspaceForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.workspaceForm)
      return;
    }
    t.setLoading(true);

    t.workspace.id = t.workspaceId;
    t.workspace.name = t.name?.value;
    t.workspace.directorUserId = t.userService.get()?.id;

    if (t.workspace.workspaceType == WorkspaceType.Personal) {
      t.workspace.country = undefined;
      t.workspace.registrationDate = undefined;
      t.workspace.address = undefined;
      t.workspace.iNN = undefined;
      t.workspace.reviewStatus = undefined;
    }
    else {
      t.workspace.country = t.country?.value;
      t.workspace.registrationDate = DatepickerUtils.dateToStr(t.registrationDate?.value);
      t.workspace.address = t.address?.value;
      t.workspace.iNN = t.TIN?.value;
      t.workspace.reviewStatus = WorkspaceReviewStatus.OnReview;
    }

    await t.workspaceService.createOrEdit(t.workspace)
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          t.activeModal.close(t.workspace);
        }
      })
      .catch((e) => {
        t.setLoading(false);
        t.showResponseError(e);
      })
  }


  back(result: boolean = false) {
    this.activeModal.close(result);
  }

}
