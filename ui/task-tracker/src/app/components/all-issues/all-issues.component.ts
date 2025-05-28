import { Component } from '@angular/core';
import { BaseComponent } from '../../shared/base/base.component';
import { UserModel } from '../../shared/model/userModel';
import { IssueService } from '../../shared/services/issue.service';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModalRef, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { UserStatusChangeType } from '../../shared/enums/user-status-change-type';
import { ProjectModel } from '../../shared/model/projectModel';
import { UserWspStatusChangeModel } from '../../shared/model/userWspStatusChangeModel';
import { WorkspaceModel } from '../../shared/model/workspaceModel';
import { AuthService } from '../../shared/services/onlyFrontServices/auth.service';
import { UserService } from '../../shared/services/user.service';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { LangPipe } from '../../shared/pipes/lang.pipe';
import { IssueFilter } from '../../shared/model/filters/issueFilter';
import { IssueModel } from '../../shared/model/issueModel';
import { CreateIssueComponent } from '../../shared/components/modals/create-issue/create-issue.component';
import { TrackTimeComponent } from '../../shared/components/modals/track-time/track-time.component';
import { TimeTrackingModel } from '../../shared/model/timeTrackingModel';
import { IssueStatus } from '../../shared/enums/issue-status';
import { TimerModel } from '../../shared/model/onlyFrontModels/timer.model';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-all-issues',
  imports: [ReactiveFormsModule, CommonModule, LangPipe],
  templateUrl: './all-issues.component.html',
  styleUrl: './all-issues.component.scss'
})
export class AllIssuesComponent extends BaseComponent {
  public workspaceId!: string;
  public projectId!: string;
  allIssues: IssueModel[] = [];
  public getUser: () => UserModel;
  modalRef!: NgbModalRef;
  UserStatusChangeType = UserStatusChangeType;
  activeTimeTrack: TimeTrackingModel | null = null;
  IssueStatus = IssueStatus;
  timerModel: TimerModel = new TimerModel();
  timerSubscription: Subscription | null = null;

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private issueService: IssueService,
    private translate: TranslateService,
    private activateRoute: ActivatedRoute,
    private router: Router,
    private userService: UserService
  ) {
    super(modalService, translate);

    this.getUser = () => {
      var user = this.userService.get() ?? new UserModel();
      return user;
    }
  }

  async ngOnInit() {
    var t = this;
    var wspIdStr = t.activateRoute.snapshot.queryParamMap.get('workspaceId');
    var projectIdStr = t.activateRoute.snapshot.queryParamMap.get('projectId');
    if (!wspIdStr || Number.isNaN(wspIdStr) || !projectIdStr || Number.isNaN(projectIdStr)) {
      t.router.navigate(['/my-workspaces']);
      return;
    }
    t.projectId = projectIdStr;
    t.workspaceId = wspIdStr;

    t.setLoading(true);
    Promise.all([
      t.getProjectIssues(false),
      t.getActiveAutoTrack(false)
    ])
      .then(() => {
        t.setLoading(false);
      });
  }

  ngOnDestroy() {
    this.pauseTimer();
  }


  public async getProjectIssues(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    var filter: IssueFilter = {
      workspaceId: +t.workspaceId,
      projectId: +t.projectId
    };

    await t.issueService.getProjectIssues(filter)
      .then((resp: any) => {
        t.allIssues = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  private async getActiveAutoTrack(needLoader: boolean = true) {
    var t = this;
    if (needLoader)
      t.setLoading(true);

    await t.issueService.getActiveAutoTrack(+t.workspaceId, +t.projectId)
      .then((resp: any) => {
        t.activeTimeTrack = resp.data;
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  public createIssue() {
    var t = this;
    t.modalRef = t.modalService.open(CreateIssueComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.workspaceId = +t.workspaceId;
    t.modalRef.componentInstance.issueProjectId = +t.projectId;

    t.modalRef.result.then(async (result) => t.processModalResult(result));
  }

  private async processModalResult(result: any) {
    if (result) {
      var t = this;
      await t.getProjectIssues(false);
      t.showSuccess("Issue sucessfully " + (!!result.id ? "updated" : "created"), "Success");
      t.setLoading(false);
    }
  }

  private async processTrackResult(result: any) {
    if (result) {
      var t = this;
      await t.getProjectIssues(false);
      t.showSuccess("Time track sucessfully recorded", "Success");
      t.setLoading(false);
    }
  }

  editIssue(issue: IssueModel) {
    var t = this;
    t.modalRef = t.modalService.open(CreateIssueComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.issueId = issue.id;
    t.modalRef.componentInstance.issueName = issue.name;
    t.modalRef.componentInstance.issueDescr = issue.description;
    t.modalRef.componentInstance.issueType = issue.type;
    t.modalRef.componentInstance.issueStatus = issue.status;
    t.modalRef.componentInstance.issuePriority = issue.priority;
    t.modalRef.componentInstance.issueEstimate = issue.estimate;
    t.modalRef.componentInstance.issueIndex = issue.index;
    t.modalRef.componentInstance.issueEpicId = issue.epicId;
    t.modalRef.componentInstance.issueAuthorId = issue.authorId;
    t.modalRef.componentInstance.issueAssigneeId = issue.assigneeId;
    t.modalRef.componentInstance.workspaceId = +t.workspaceId;
    t.modalRef.componentInstance.issueProjectId = +t.projectId;

    t.modalRef.result.then(async (result) => t.processModalResult(result));
  }

  trackTime(issueId: number) {
    var t = this;
    t.modalRef = t.modalService.open(TrackTimeComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.issueId = issueId;

    t.modalRef.result.then(async (result) => t.processTrackResult(result));
  }

  startAutoTracking(issueId: number) {
    var t = this;
    t.showConfirm("Are you sure you want to start automatic time tracking?", "Confirm the action", true, "Yes")
      .then(async (result) => t.processTrackResult(result));
  }

  stopAutoTracking(issueId: number) {
    var t = this;
    t.showConfirm("Are you sure you want to stop automatic time tracking?", "Confirm the action", true, "Yes")
      .then(async (result) => t.processTrackResult(result));
  }

  startTimer() {
    if (!this.timerModel.timerRunning) {
      this.timerModel.timerRunning = true;
      this.timerSubscription = interval(1000).subscribe(() => {
        this.timerModel.seconds++;
        if (this.timerModel.seconds >= 60) {
          this.timerModel.seconds = 0;
          this.timerModel.minutes++;
          if (this.timerModel.minutes >= 60) {
            this.timerModel.minutes = 0;
            this.timerModel.hours++;
            if (this.timerModel.hours >= 24) {
              this.timerModel.hours = 0;
            }
          }
        }
      });
    }
  }

  pauseTimer() {
    if (this.timerSubscription) {
      this.timerSubscription.unsubscribe();
      this.timerSubscription = null;
      this.timerModel.timerRunning = false;
    }
  }

  resetTimer() {
    this.pauseTimer();
    this.timerModel.seconds = 0;
    this.timerModel.minutes = 0;
    this.timerModel.hours = 0;
  }
}
