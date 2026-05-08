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
import { AutoTrackTimeStatus } from '../../shared/enums/auto-track-time-status';
import { AutoTimeTrackService } from '../../shared/services/autoTimeTrack.service';
import { TimeTrackPR } from '../../shared/model/postRequests/timeTrackPR';
import { DatepickerUtils } from '../../shared/utils/ngbDatepickerUtils';
import { DateUtils } from '../../shared/utils/dateUtils';
import { FormatDatePipe } from "../../shared/pipes/formatDate.pipe";
import { AutoTimeTrackPR } from '../../shared/model/postRequests/autoTimeTrackPR';
import { CreateOrEditIssuePR } from '../../shared/model/postRequests/createOrEditIssuePR';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-all-issues',
  imports: [ReactiveFormsModule, CommonModule, LangPipe, FormatDatePipe, DragDropModule],
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
  activeIssue: IssueModel | undefined;
  AutoTrackTimeStatus = AutoTrackTimeStatus;
  isKanbanView: boolean = false;
  kanbanStatuses: IssueStatus[] = [
    IssueStatus.Backlog,
    IssueStatus.SelectedForDevelopment,
    IssueStatus.InProgress,
    IssueStatus.PullRequest,
    IssueStatus.ToDeploy,
    IssueStatus.Test,
    IssueStatus.Declined,
    IssueStatus.Done,
    IssueStatus.Deferred
  ];
  kanbanIssuesByStatus: { [key: number]: IssueModel[] } = {};

  constructor(
    public authService: AuthService,
    private modalService: NgbModal,
    private issueService: IssueService,
    private translate: TranslateService,
    private activateRoute: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private autoTimeTrackService: AutoTimeTrackService
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
        t.activeIssue = t.allIssues.find(x=> x.id == t.activeTimeTrack?.issueId);
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
        t.refreshKanbanColumns();
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

    await t.autoTimeTrackService.getActiveAutoTrack(+t.projectId, +t.workspaceId)
      .then((resp: any) => {
        t.activeTimeTrack = resp.data;
        t.initTimer();
      })
      .catch((e) => {
        t.showResponseError(e);
      })
      .finally(() => {
        if (needLoader)
          t.setLoading(false);
      });
  }

  private initTimer() {
    var t = this;
    if (t.activeTimeTrack) {
      switch(t.activeTimeTrack.autoTrackStatus){
        case AutoTrackTimeStatus.Active:
          //Если статус активный, значит ставим считаем значение по BeginDate и ставим таймер
          var timeSpent = DateUtils.timeSince(t.activeTimeTrack.dateBegin);
          var timeArr = timeSpent.split(':');
          t.timerModel.hours = parseInt(timeArr[0]);
          t.timerModel.minutes = parseInt(timeArr[1]);
          t.timerModel.seconds = parseInt(timeArr[2]);
          t.startTimer();
          break;
        case AutoTrackTimeStatus.Stopped:
          var timeArr = t.activeTimeTrack.timeSpent.split(':');
          t.timerModel.hours = parseInt(timeArr[0]);
          t.timerModel.minutes = parseInt(timeArr[1]);
          t.timerModel.seconds = parseInt(timeArr[2]);
          break;
      }
    }
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
      await t.getActiveAutoTrack(false);
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
      .then(async (result) => {
        if (result) {
          t.setLoading(true);
          var timeTrackPr = new AutoTimeTrackPR();
          timeTrackPr.timeSpent = "0h 0m 0s";
          timeTrackPr.dateBegin = new Date().toISOString().split(".")[0];
          timeTrackPr.userId = t.getUser().id;
          timeTrackPr.issueId = issueId;
          await t.autoTimeTrackService.startTracking(timeTrackPr)
            .then((resp: any) => {
              t.activeTimeTrack = resp.data;
              t.initTimer();
            })
            .catch((e) => {
              t.showResponseError(e);
            })
            .finally(() => {
              t.setLoading(false);
            });
        }
      });
  }

  stopAutoTracking(issueId: number) {
    var t = this;
    t.showConfirm("Are you sure you want to stop automatic time tracking?", "Confirm the action", true, "Yes")
      .then(async (result) => {
        if (result) {
          t.pauseTimer();
          t.setLoading(true);
          var timeTrackPr = new AutoTimeTrackPR();
          timeTrackPr.timeSpent = `${t.timerModel.hours}h ${t.timerModel.minutes}m ${t.timerModel.seconds}s`;
          timeTrackPr.userId = t.getUser().id;
          timeTrackPr.issueId = t.activeTimeTrack?.issueId ?? t.activeIssue?.id ?? issueId;
          timeTrackPr.id = t.activeTimeTrack?.id;
          await t.autoTimeTrackService.stopTracking(timeTrackPr)
            .then((resp: any) => {
              t.activeTimeTrack = resp.data;
            })
            .catch((e) => {
              t.showResponseError(e);
            })
            .finally(() => {
              t.setLoading(false);
            });
        }
      });
  }

  finishAutoTracking(){
    var t = this;
    t.modalRef = t.modalService.open(TrackTimeComponent,
      {
        centered: true,
        size: 'lg'
      });

    t.modalRef.componentInstance.id = t.activeTimeTrack?.id;
    t.modalRef.componentInstance.issueId = t.activeTimeTrack?.issueId;
    t.modalRef.componentInstance.dateBegin = t.activeTimeTrack?.dateBegin;
    t.modalRef.componentInstance.timeSpent = `${t.timerModel.hours}h ${t.timerModel.minutes}m ${t.timerModel.seconds}s`;

    t.modalRef.result.then(async (result) => t.processTrackResult(result));
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

  public setViewMode(isKanbanView: boolean) {
    this.isKanbanView = isKanbanView;
  }

  public getDropListId(status: IssueStatus): string {
    return `kanban-drop-${status}`;
  }

  public get kanbanDropListIds(): string[] {
    return this.kanbanStatuses.map((status) => this.getDropListId(status));
  }

  public onDrop(event: CdkDragDrop<IssueModel[]>, targetStatus: IssueStatus) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    var movedIssue = event.previousContainer.data[event.previousIndex];
    if (!movedIssue) {
      return;
    }

    this.updateIssueStatus(movedIssue, targetStatus);
  }

  public getIssuesByStatus(status: IssueStatus): IssueModel[] {
    return this.kanbanIssuesByStatus[status] ?? [];
  }

  public canMoveLeft(status: IssueStatus): boolean {
    return this.kanbanStatuses.indexOf(status) > 0;
  }

  public canMoveRight(status: IssueStatus): boolean {
    var statusIndex = this.kanbanStatuses.indexOf(status);
    return statusIndex > -1 && statusIndex < this.kanbanStatuses.length - 1;
  }

  public moveIssue(issue: IssueModel, direction: number) {
    var t = this;
    var currentIndex = t.kanbanStatuses.indexOf(issue.status);
    if (currentIndex < 0) {
      return;
    }

    var nextIndex = currentIndex + direction;
    if (nextIndex < 0 || nextIndex >= t.kanbanStatuses.length) {
      return;
    }

    t.updateIssueStatus(issue, t.kanbanStatuses[nextIndex]);
  }

  private refreshKanbanColumns() {
    var t = this;
    var groupedIssues: { [key: number]: IssueModel[] } = {};
    t.kanbanStatuses.forEach((status) => {
      groupedIssues[status] = t.allIssues.filter((issue) => issue.status === status);
    });
    t.kanbanIssuesByStatus = groupedIssues;
  }

  private async updateIssueStatus(issue: IssueModel, newStatus: IssueStatus): Promise<boolean> {
    var t = this;
    if (issue.status === newStatus) {
      return true;
    }

    t.setLoading(true);
    var issuePr = new CreateOrEditIssuePR();
    issuePr.id = issue.id;
    issuePr.name = issue.name;
    issuePr.description = issue.description;
    issuePr.type = issue.type;
    issuePr.status = newStatus;
    issuePr.priority = issue.priority;
    issuePr.estimate = issue.estimate;
    issuePr.index = issue.index;
    issuePr.epicId = issue.epicId;
    issuePr.authorId = issue.authorId;
    issuePr.assigneeId = issue.assigneeId;
    issuePr.projectId = issue.projectId;

    await t.issueService.createOrEdit(issuePr)
      .then(async (resp: any) => {
        if (!!resp && !!resp.data) {
          issue.status = newStatus;
          if (t.activeIssue?.id === issue.id) {
            t.activeIssue.status = newStatus;
          }
          t.refreshKanbanColumns();
          return true;
        }
        return false;
      })
      .catch((e) => {
        t.showResponseError(e);
        return false;
      })
      .finally(() => {
        t.setLoading(false);
      });

    return issue.status === newStatus;
  }
}
