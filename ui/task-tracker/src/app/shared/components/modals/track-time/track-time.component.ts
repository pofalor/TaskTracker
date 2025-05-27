import { Component, Input } from '@angular/core';
import { TimeTrackPR } from '../../../model/postRequests/timeTrackPR';
import { BaseComponent } from '../../../base/base.component';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgbDatepickerModule, NgbModal, NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { NgSelectComponent, NgOptionComponent } from '@ng-select/ng-select';
import { TranslateService } from '@ngx-translate/core';
import { WorkspaceType } from '../../../enums/work-space-type';
import { LangPipe } from '../../../pipes/lang.pipe';
import { UserService } from '../../../services/user.service';
import { WorkspaceService } from '../../../services/workspace.service';
import { IssueService } from '../../../services/issue.service';
import { DatepickerUtils } from '../../../utils/ngbDatepickerUtils';

@Component({
  selector: 'app-track-time',
  imports: [CommonModule, LangPipe, ReactiveFormsModule, NgbDatepickerModule],
  templateUrl: './track-time.component.html',
  styleUrl: './track-time.component.scss'
})
export class TrackTimeComponent extends BaseComponent {
  @Input() issueId!: number;
  @Input() timeSpent: string = '';
  @Input() dateBegin: any;
  @Input() comment: string | undefined;
  public timeTrackForm!: FormGroup;
  timeTrack: TimeTrackPR = new TimeTrackPR();

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private activeModal: NgbActiveModal,
    private issueService: IssueService,
    private translate: TranslateService,
    private userService: UserService,
  ) {
    super(modalService, translate);
  }

  get formTimeSpent() { return this.timeTrackForm.get('formTimeSpent'); }
  get formDateBegin() { return this.timeTrackForm.get('formDateBegin'); }
  get formComment() { return this.timeTrackForm.get('formComment'); }

  ngOnInit() {
    var t = this;
    this.timeTrackForm = t.fb.group({
      formTimeSpent: [t.timeSpent, [Validators.required, Validators.pattern(t.timeTrackPattern)]],
      formDateBegin: [t.dateBegin, [t.baseDateValidator.bind(t)]],
      formComment: [t.comment, [Validators.maxLength(500)]]
    });
  }

  back(result: boolean = false) {
    this.activeModal.close(result);
  }

  async createTimeTrack() {
    var t = this;
    if (t.timeTrackForm.invalid) {
      t.markFormGroupTouchedAndDirty(t.timeTrackForm)
      return;
    }
    t.setLoading(true);

    t.timeTrack.issueId = t.issueId;
    t.timeTrack.timeSpent =  t.formTimeSpent?.value;
    t.timeTrack.dateBegin = !!t.formDateBegin?.value ? DatepickerUtils.dateToStr(t.formDateBegin?.value) : undefined;
    t.timeTrack.comment = t.formComment?.value;

    await t.issueService.trackTime(t.timeTrack)
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          t.activeModal.close(t.timeTrack);
        }
      })
      .catch((e) => {
        t.setLoading(false);
        t.showResponseError(e);
      })
  }
}
