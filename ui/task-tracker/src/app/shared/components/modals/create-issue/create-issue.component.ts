import { Component, Input, OnDestroy } from '@angular/core';
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
import { IssueModel } from '../../../model/issueModel';
import { IssueFilter } from '../../../model/filters/issueFilter';
import { formatTimeSpentForInput } from '../../../utils/timeTrackUtils';
import { IssueEstimatePredictionModel } from '../../../model/issueEstimatePredictionModel';
import { IssueEstimatePredictionPR } from '../../../model/postRequests/issueEstimatePredictionPR';
import { Subject, debounceTime, takeUntil } from 'rxjs';

@Component({
  selector: 'app-create-issue',
  imports: [CommonModule, LangPipe, ReactiveFormsModule,
    NgSelectComponent, NgOptionComponent, NgbDatepickerModule, NgbAlertModule],
  templateUrl: './create-issue.component.html',
  styleUrl: './create-issue.component.scss'
})
export class CreateIssueComponent extends BaseComponent implements OnDestroy {
  @Input() issueId: number | undefined;
  @Input() issueName: string | undefined = '';
  @Input() issueDescr: string | undefined = '';
  @Input() issueType: IssueType | undefined;
  @Input() issueStatus: IssueStatus | undefined;
  @Input() issuePriority: IssuePriority | undefined;
  @Input() issueEstimate: string | undefined;
  @Input() issueIndex: number | undefined;
  @Input() issueParentId: number | undefined;
  @Input() issueAuthorId: number | undefined;
  @Input() issueAssigneeId: number | undefined;
  @Input() issueProjectId!: number;
  @Input() workspaceId!: number;
  public issueForm!: FormGroup;
  issue: CreateOrEditIssuePR = new CreateOrEditIssuePR();
  assigneeCandidates: UserModel[] = [];
  parentIssueCandidates: IssueModel[] = [];
  issueTypeArray: { label: string; value: string }[] = [];
  IssueStatuses = IssueStatuses;
  issuePriorityArray: { label: string; value: string }[] = [];
  estimatePrediction: IssueEstimatePredictionModel | undefined;
  estimatePredictionLoading = false;
  showEstimatePredictionDetails = false;
  private estimatePredictionRequestId = 0;
  private destroy$ = new Subject<void>();

  get formIssueName() { return this.issueForm.get('formIssueName'); }
  get formIssueType() { return this.issueForm.get('formIssueType'); }
  get formIssueStatus() { return this.issueForm.get('formIssueStatus'); }
  get formIssuePriority() { return this.issueForm.get('formIssuePriority'); }
  get formIssueEstimate() { return this.issueForm.get('formIssueEstimate'); }
  get formIssueDescr() { return this.issueForm.get('formIssueDescr'); }
  get formIssueParentId() { return this.issueForm.get('formIssueParentId'); }
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
      formIssueType: [t.issueType],
      formIssueStatus: [t.issueStatus],
      formIssuePriority: [t.issuePriority],
      formIssueEstimate: [formatTimeSpentForInput(t.issueEstimate), [Validators.pattern(t.timeTrackPattern)]],
      formIssueDescr: [t.issueDescr, [Validators.maxLength(500)]],
      formIssueParentId: [t.issueParentId, []],
      formIssueAssigneeId: [t.issueAssigneeId],
    });

    await Promise.all([
      t.getAssigneeCandidates(false),
      t.loadParentIssueCandidates(false),
    ]);
    t.convertEnumToArr(IssueType, t.issueTypeArray);
    t.convertEnumToArr(IssuePriority, t.issuePriorityArray);

    t.issueForm.valueChanges
      .pipe(debounceTime(600), takeUntil(t.destroy$))
      .subscribe(() => {
        void t.loadEstimatePrediction();
      });

    void t.loadEstimatePrediction();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getIssueKey(issue: IssueModel): string {
    return `${issue.projectCode}-${issue.index}`;
  }

  async loadParentIssueCandidates(needLoader: boolean = true) {
    var t = this;
    if (needLoader) {
      t.setLoading(true);
    }

    const filter: IssueFilter = {
      workspaceId: +t.workspaceId,
      projectId: +t.issueProjectId,
    };

    await t.issueService.getProjectIssues(filter)
      .then((resp: any) => {
        t.parentIssueCandidates = (resp.data ?? []).filter((issue: IssueModel) => issue.id !== t.issueId);
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader) {
          t.setLoading(false);
        }
      });
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

  async loadEstimatePrediction() {
    var t = this;
    const request = t.buildEstimatePredictionRequest();

    if (!request) {
      t.estimatePrediction = undefined;
      return;
    }

    const requestId = ++t.estimatePredictionRequestId;
    t.estimatePredictionLoading = true;

    await t.issueService.predictEstimate(request)
      .then((resp: any) => {
        if (requestId !== t.estimatePredictionRequestId) {
          return;
        }

        t.estimatePrediction = resp.data;
      })
      .catch(() => {
        if (requestId === t.estimatePredictionRequestId) {
          t.estimatePrediction = undefined;
        }
      })
      .finally(() => {
        if (requestId === t.estimatePredictionRequestId) {
          t.estimatePredictionLoading = false;
        }
      });
  }

  buildEstimatePredictionRequest(): IssueEstimatePredictionPR | undefined {
    var t = this;
    const projectId = +t.issueProjectId;

    if (!projectId) {
      return undefined;
    }

    const request = new IssueEstimatePredictionPR();
    request.id = t.issueId;
    request.name = t.formIssueName?.value ?? '';
    request.description = t.formIssueDescr?.value ?? '';
    request.type = t.formIssueType?.value ?? IssueType.Task;
    request.status = t.formIssueStatus?.value ?? IssueStatus.Backlog;
    request.priority = t.formIssuePriority?.value ?? IssuePriority.Medium;
    request.parentId = t.formIssueParentId?.value ?? undefined;
    request.assigneeId = t.formIssueAssigneeId?.value ?? t.userService.get()?.id;
    request.projectId = projectId;

    return request;
  }

  applyPredictedEstimate() {
    var t = this;
    if (!t.estimatePrediction?.estimate) {
      return;
    }

    t.formIssueEstimate?.setValue(t.estimatePrediction.estimate, { emitEvent: false });
    t.formIssueEstimate?.markAsDirty();
    t.formIssueEstimate?.markAsTouched();
  }

  toggleEstimatePredictionDetails() {
    this.showEstimatePredictionDetails = !this.showEstimatePredictionDetails;
  }

  formatPredictionTime(totalSeconds: number | undefined): string {
    if (!totalSeconds || totalSeconds <= 0) {
      return '';
    }

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    const useRu = this.getCurrentLocalization() === 'ru';
    const parts: string[] = [];

    if (hours > 0) {
      parts.push(useRu ? `${hours} ${this.pluralRu(hours, ['час', 'часа', 'часов'])}` : `${hours} ${hours === 1 ? 'hour' : 'hours'}`);
    }

    if (minutes > 0) {
      parts.push(useRu ? `${minutes} ${this.pluralRu(minutes, ['минута', 'минуты', 'минут'])}` : `${minutes} ${minutes === 1 ? 'minute' : 'minutes'}`);
    }

    if (seconds > 0) {
      parts.push(useRu ? `${seconds} ${this.pluralRu(seconds, ['секунда', 'секунды', 'секунд'])}` : `${seconds} ${seconds === 1 ? 'second' : 'seconds'}`);
    }

    return parts.join(' ');
  }

  formatPredictionConfidence(confidence: number | undefined): string {
    return `${Math.round((confidence ?? 0) * 100)}%`;
  }

  private pluralRu(value: number, forms: [string, string, string]): string {
    const absValue = Math.abs(value) % 100;
    const lastDigit = absValue % 10;

    if (absValue > 10 && absValue < 20) {
      return forms[2];
    }

    if (lastDigit > 1 && lastDigit < 5) {
      return forms[1];
    }

    if (lastDigit === 1) {
      return forms[0];
    }

    return forms[2];
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
    t.issue.type = t.formIssueType?.value ?? IssueType.Task;
    t.issue.status = t.formIssueStatus?.value ?? IssueStatus.Backlog;
    t.issue.priority = t.formIssuePriority?.value ?? IssuePriority.Medium;
    t.issue.estimate = t.formIssueEstimate?.value?.trim() || undefined;
    t.issue.authorId = t.userService.get()?.id;
    t.issue.parentId = t.formIssueParentId?.value ?? undefined;
    t.issue.assigneeId = t.formIssueAssigneeId?.value ?? t.userService.get()?.id;
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
