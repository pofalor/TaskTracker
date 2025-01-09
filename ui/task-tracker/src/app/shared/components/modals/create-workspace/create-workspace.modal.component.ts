import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../base/base.component';
import { NgbActiveModal, NgbAlertModule, NgbDatepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { WorkSpaceType } from '../../../enums/work-space-type';
import { CreateOrEditWorkSpacePostRequest } from '../../../model/postRequests/createOrEditWorkSpacePostRequest';
import { LangPipe } from '../../../pipes/lang.pipe';
import { CommonModule } from '@angular/common';
import { ListCountry } from '../../../constants/country';
import { NgOptionComponent, NgSelectComponent } from '@ng-select/ng-select';
import { WorkspaceService } from '../../../services/workspace.service';
import { TranslateService } from '@ngx-translate/core';
import { UserModel } from '../../../model/userModel';
import { UserService } from '../../../services/user.service';
import { DatepickerUtils } from '../../../utils/ngbDatepickerUtils';


@Component({
  selector: 'app-create-workspace-modal',
  imports: [CommonModule, LangPipe, ReactiveFormsModule,
    NgSelectComponent, NgOptionComponent, NgbAlertModule, NgbDatepickerModule,],
  templateUrl: './create-workspace.modal.component.html',
  styleUrl: './create-workspace.modal.component.scss'
})
export class CreateWorkspaceModalComponent extends BaseComponent {
  @Input() workSpaceId: number | undefined;
  @Input() workSpaceName: string = '';
  @Input() workSpaceType: WorkSpaceType = WorkSpaceType.Personal;
  @Input() workSpaceCountry: number | undefined;
  @Input() workSpaceRegisterDate: string | undefined = '';
  @Input() workSpaceAddress: string | undefined = '';
  @Input() workSpaceINN: string | undefined;
  public workSpaceForm!: FormGroup;
  workSpace: CreateOrEditWorkSpacePostRequest = new CreateOrEditWorkSpacePostRequest();
  WorkSpaceType = WorkSpaceType;
  public countryList: any[] = ListCountry;
  maxNameLength: number = 15;

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private activeModal: NgbActiveModal,
    private workSpaceService: WorkspaceService,
    private translate: TranslateService,
    private userService: UserService,
  ) {
    super(modalService, translate);

    var t = this;

  }

  get name() { return this.workSpaceForm.get('name'); }
  get country() { return this.workSpaceForm.get('country'); }
  get registrationDate() { return this.workSpaceForm.get('registrationDate'); }
  get address() { return this.workSpaceForm.get('address'); }
  get TIN() { return this.workSpaceForm.get('TIN'); }

  ngOnInit() {
    var t = this;
    t.workSpace.workSpaceType = t.workSpaceType;
    this.workSpaceForm = t.fb.group({
      name: [t.workSpaceName, [Validators.required, Validators.pattern(t.namePattern), Validators.maxLength(t.maxNameLength)]],
      country: [{ value: t.workSpaceCountry, disabled: true }, [Validators.required, Validators.min(0), Validators.max(279)]],
      registrationDate: [{ value: t.workSpaceRegisterDate, disabled: true }, [Validators.required, t.dateValidator.bind(t)]],
      address: [{ value: t.workSpaceAddress, disabled: true }, [Validators.required, Validators.maxLength(300)]],
      TIN: [{ value: t.workSpaceINN, disabled: true }, [Validators.required, Validators.maxLength(50)]],
    });

    if(t.workSpace.workSpaceType == WorkSpaceType.Company)
      t.changeWorkSpaceType(t.workSpace.workSpaceType);
  }

  dateValidator() {
    return this.baseDateValidator(this.workSpaceForm, 'registrationDate');
  }

  changeWorkSpaceType(workSpaceType: WorkSpaceType) {
    var t = this;
    t.workSpace.workSpaceType = workSpaceType;
    switch (workSpaceType) {
      case WorkSpaceType.Company:
        t.country?.enable();
        t.registrationDate?.enable();
        t.address?.enable();
        t.TIN?.enable();
        break;
      case WorkSpaceType.Personal:
        t.country?.disable();
        t.registrationDate?.disable();
        t.address?.disable();
        t.TIN?.disable();
        break;
    }
  }

  async createOrEditWorkspace() {
    var t = this;
    if (t.workSpaceForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.workSpaceForm)
      return;
    }
    t.setLoading(true);

    t.workSpace.id = t.workSpaceId;
    t.workSpace.name = t.name?.value;
    t.workSpace.directorUserId = t.userService.get()?.id;

    if (t.workSpace.workSpaceType == WorkSpaceType.Personal) {
      t.workSpace.country = undefined;
      t.workSpace.registrationDate = undefined;
      t.workSpace.address = undefined;
      t.workSpace.iNN = undefined;
    }
    else {
      t.workSpace.country = t.country?.value;
      t.workSpace.registrationDate = DatepickerUtils.dateToStr(t.registrationDate?.value);
      t.workSpace.address = t.address?.value;
      t.workSpace.iNN = t.TIN?.value;
    }

    await t.workSpaceService.createOrEdit(t.workSpace)
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          t.activeModal.close(t.workSpace);
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
