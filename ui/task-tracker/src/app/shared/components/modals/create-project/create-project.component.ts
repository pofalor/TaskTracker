import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../base/base.component';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateOrEditWorkSpacePostRequest } from '../../../model/postRequests/createOrEditWorkSpacePostRequest';
import { ProjectService } from '../../../services/project.service';
import { NgbModal, NgbActiveModal, NgbDatepickerModule, NgbAlertModule } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { UserService } from '../../../services/user.service';
import { UserModel } from '../../../model/userModel';
import { CommonModule } from '@angular/common';
import { LangPipe } from '../../../pipes/lang.pipe';
import { NgSelectComponent, NgOptionComponent } from '@ng-select/ng-select';
import { CreateOrEditProjectPR } from '../../../model/postRequests/createOrEditProjectPR';
import { DatepickerUtils } from '../../../utils/ngbDatepickerUtils';

@Component({
  selector: 'app-create-project',
  imports: [CommonModule, LangPipe, ReactiveFormsModule,
    NgSelectComponent, NgOptionComponent, NgbDatepickerModule, NgbAlertModule],
  templateUrl: './create-project.component.html',
  styleUrl: './create-project.component.scss'
})
export class CreateProjectComponent extends BaseComponent {
  @Input() projectId: number | undefined;
  @Input() projectName: string | undefined = '';
  @Input() projectDescr: string | undefined = '';
  @Input() projectCode: string | undefined = '';
  @Input() projectStartDate: any | undefined = null;
  @Input() projectEndDate: any | undefined = null;
  @Input() projectAuthorId: number | undefined;
  @Input() projectMgrId: number | undefined;
  @Input() workspaceId!: number;
  public projectForm!: FormGroup;
  project: CreateOrEditProjectPR = new CreateOrEditProjectPR();
  projectMgrCandidates: UserModel[] = [];

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private activeModal: NgbActiveModal,
    private translate: TranslateService,
    private userService: UserService,
    private projectService: ProjectService,
  ) {
    super(modalService, translate);
  }

  get formProjectName() { return this.projectForm.get('formProjectName'); }
  get formProjectCode() { return this.projectForm.get('formProjectCode'); }
  get formProjectStartDate() { return this.projectForm.get('formProjectStartDate'); }
  get formProjectEndDate() { return this.projectForm.get('formProjectEndDate'); }
  get formProjectMgrId() { return this.projectForm.get('formProjectMgrId'); }
  get formProjectDescr() { return this.projectForm.get('formProjectDescr'); }

  async ngOnInit() {
    var t = this;
    this.projectForm = t.fb.group({
      formProjectName: [t.projectName, [Validators.required, Validators.maxLength(50)]],
      formProjectCode: [t.projectCode, [Validators.required, Validators.maxLength(5)]],
      formProjectStartDate: [t.projectStartDate, [Validators.required, t.baseDateValidator.bind(t)]],
      formProjectEndDate: [t.projectEndDate, [t.baseDateValidator.bind(t)]],
      formProjectMgrId: [t.projectMgrId, [Validators.required, Validators.min(1)]],
      formProjectDescr: [t.projectDescr, [Validators.required, Validators.maxLength(500)]],
    });

    await t.getProjectMgrCandidates(true);
  }
  
  async getProjectMgrCandidates(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.projectService.getProjectMgrCandidates(+t.workspaceId)
      .then((resp: any) => {
        t.projectMgrCandidates = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  back(result: boolean = false) {
    this.activeModal.close(result);
  }

  async createOrEditProject() {
    var t = this;
    if (t.projectForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.projectForm)
      return;
    }
    t.setLoading(true);

    t.project.id = t.projectId;
    t.project.name = t.formProjectName?.value;
    t.project.description = t.formProjectDescr?.value;
    t.project.code = t.formProjectCode?.value;
    t.project.startDate = DatepickerUtils.dateToStr(t.formProjectStartDate?.value);
    var endDateObj = t.formProjectEndDate?.value;
    t.project.endDate = endDateObj ? DatepickerUtils.dateToStr(endDateObj) : undefined;
    t.project.authorId = t.userService.get()?.id;
    t.project.projectMgrId = t.formProjectMgrId?.value;
    t.project.workSpaceId = t.workspaceId;

    await t.projectService.createOrEdit(t.project)
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          t.activeModal.close(t.project);
        }
      })
      .catch((e) => {
        t.setLoading(false);
        t.showResponseError(e);
      })
  }

  setCode() {
    var t = this;
    if (!!t.projectCode) {
      return;
    }
    var code = t.convertNameToCode();
    t.formProjectCode?.setValue(code);
  }

  convertNameToCode(): string {
    var name = this.formProjectName?.value;
    if (!name) {
      return ""; // Handle empty or null names
    }

    const vowels = "aeiouyаеёиоуыэюя";
    const isVowel = (char: string) => vowels.includes(char.toLowerCase());

    // 1. Split into words
    const words = name.trim().split(/\s+/);

    if (words.length > 1) {
      let code = "";
      if (words.length === 2) {
        const firstWord = words[0].toLowerCase();
        const secondWord = words[1].toLowerCase();
        let firstConsonant1 = "";
        const foundFirstConsonant1 = firstWord.split("").find((char: string) => !isVowel(char));
        if (foundFirstConsonant1) {
          firstConsonant1 = foundFirstConsonant1
        }


        let firstConsonant2 = "";
        const foundFirstConsonant2 = secondWord.split("").find((char: string) => !isVowel(char));
        if (foundFirstConsonant2) {
          firstConsonant2 = foundFirstConsonant2
        }
        if (firstConsonant1 && firstConsonant2) {
          code = firstConsonant1 + firstConsonant2;
        } else {
          code = firstWord.substring(0, 1) + secondWord.substring(0, 1)
        }
      } else {
        code = words
          .map((word: string) => word.substring(0, 1).toLowerCase())
          .join("")
          .substring(0, 5);
      }
      return code.toUpperCase();
    }

    // 2. Single word
    const consonants = name.split("").filter((char: string) => !isVowel(char)).join("");
    const otherChars = name.split("").filter((char: string, index: any) => isVowel(char) && consonants.length < 5).join("");

    if (consonants.length > 0) {
      let code = "";
      for (let i = 0; code.length < 5 && (i < consonants.length || i < otherChars.length); i++) {

        if (i < consonants.length) {
          code += consonants[i];
        }
        if (code.length < 5 && i < otherChars.length) {
          code += otherChars[i];
        }
      }

      return code.substring(0, 5).toUpperCase();
    }

    // 3. Short name case or no vowels, or single letter and others problems
    if (name.length <= 5) {
      return name.toUpperCase();
    } else {
      return name.substring(0, 5).toUpperCase();
    }
  }
}
