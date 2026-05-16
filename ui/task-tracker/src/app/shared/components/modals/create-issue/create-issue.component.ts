import { Component, Input } from '@angular/core';
import { CreateOrEditIssuePR } from '../../../model/postRequests/createOrEditIssuePR';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgbDatepickerModule, NgbAlertModule, NgbModal, NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { NgSelectComponent, NgOptionComponent } from '@ng-select/ng-select';
import { TranslateService } from '@ngx-translate/core';
import { UserModel } from '../../../model/userModel';
import { LangPipe } from '../../../pipes/lang.pipe';
import { ProjectService } from '../../../services/project.service';
import { UserService } from '../../../services/user.service';
import { BaseComponent } from '../../../base/base.component';
import { IssueType } from '../../../enums/issue-type';
import { IssuePriority } from '../../../enums/issue-priority';
import { IssueStatus } from '../../../enums/issue-status';
import { IssueService } from '../../../services/issue.service';
import { IssueStatuses } from '../../../constants/issue-statuses';

@Component({
  selector: 'app-create-issue',
  imports: [CommonModule, LangPipe, ReactiveFormsModule,
    NgSelectComponent, NgOptionComponent, NgbDatepickerModule, NgbAlertModule],
  templateUrl: './create-issue.component.html',
  styleUrl: './create-issue.component.scss'
})
export class CreateIssueComponent extends BaseComponent {
  @Input() issueId: number | undefined;
  @Input() issueName: string | undefined = '';
  @Input() issueDescr: string | undefined = '';
  @Input() issueType: IssueType | undefined;
  @Input() issueStatus: IssueStatus | undefined;
  @Input() issuePriority: IssuePriority | undefined;
  @Input() issueEstimate: string | undefined;
  @Input() issueIndex: number | undefined;
  @Input() issueEpicId: number | undefined;
  @Input() issueAuthorId: number | undefined;
  @Input() issueAssigneeId: number | undefined;
  @Input() issueProjectId!: number;
  @Input() workspaceId!: number;
  public issueForm!: FormGroup;
  issue: CreateOrEditIssuePR = new CreateOrEditIssuePR();
  assigneeCandidates: UserModel[] = [];
  issueTypeArray: { label: string; value: string }[] = [];
  IssueStatuses = IssueStatuses;
  issuePriorityArray: { label: string; value: string }[] = [];

  get formIssueName() { return this.issueForm.get('formIssueName'); }
  get formIssueType() { return this.issueForm.get('formIssueType'); }
  get formIssueStatus() { return this.issueForm.get('formIssueStatus'); }
  get formIssuePriority() { return this.issueForm.get('formIssuePriority'); }
  get formIssueEstimate() { return this.issueForm.get('formIssueEstimate'); }
  get formIssueDescr() { return this.issueForm.get('formIssueDescr'); }
  get formIssueEpicId() { return this.issueForm.get('formIssueEpicId'); }
  get formIssueAssigneeId() { return this.issueForm.get('formIssueAssigneeId'); }

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private activeModal: NgbActiveModal,
    private translate: TranslateService,
    private userService: UserService,
    private issueService: IssueService,
    private projectService: ProjectService,
  ) {
    super(modalService, translate);
  }

  async ngOnInit() {
    var t = this;
    this.issueForm = t.fb.group({
      formIssueName: [t.issueName, [Validators.required, Validators.maxLength(50)]],
      formIssueType: [t.issueType, [Validators.required]],
      formIssueStatus: [t.issueStatus, [Validators.required]],
      formIssuePriority: [t.issuePriority, [Validators.required]],
      formIssueEstimate: [t.issueEstimate, []],
      formIssueDescr: [t.issueDescr, [Validators.required, Validators.maxLength(500)]],
      formIssueEpicId: [t.issueEpicId, []],
      formIssueAssigneeId: [t.issueAssigneeId, [Validators.required, Validators.min(1)]],
    });

    await t.getAssigneeCandidates(true);
    t.convertEnumToArr(IssueType, t.issueTypeArray);
    t.convertEnumToArr(IssuePriority, t.issuePriorityArray);
  }

  async getAssigneeCandidates(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.projectService.getProjectMgrCandidates(+t.workspaceId)
      .then((resp: any) => {
        t.assigneeCandidates = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  convertEnumToArr(enumVal: any, resultModel: { label: string; value: string }[]) {
    for (const key in enumVal) {
      if (enumVal.hasOwnProperty(key) && isNaN(Number(key)) === true && enumVal[key] != -1) {
        resultModel.push({ label: key, value: enumVal[key] });
      }
    }
  }

  back(result: boolean = false) {
    this.activeModal.close(result);
  }

  async createOrEditIssue() {
    var t = this;
    if (t.issueForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.issueForm)
      return;
    }
    t.setLoading(true);

    t.issue.id = t.issueId;
    t.issue.name = t.formIssueName?.value;
    t.issue.description = t.formIssueDescr?.value;
    t.issue.type = t.formIssueType?.value;
    t.issue.status = t.formIssueStatus?.value;
    t.issue.priority = t.formIssuePriority?.value;
    t.issue.authorId = t.userService.get()?.id;
    t.issue.epicId = t.formIssueEpicId?.value;
    t.issue.assigneeId = t.formIssueAssigneeId?.value;
    t.issue.projectId = t.issueProjectId;

    const saveIssue = t.issueId ? t.issueService.update(t.issue) : t.issueService.create(t.issue);
    await saveIssue
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          t.activeModal.close(t.issue);
        }
      })
      .catch((e) => {
        t.setLoading(false);
        t.showResponseError(e);
      })
  }
}
