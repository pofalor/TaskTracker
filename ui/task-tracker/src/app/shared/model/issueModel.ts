import { IssuePriority } from "../enums/issue-priority";
import { IssueStatus } from "../enums/issue-status";
import { IssueType } from "../enums/issue-type";

export class IssueModel {
    id!: number;
    projectCode!: string;
    name!: string;
    description!: string;
    type!: IssueType;
    status!: IssueStatus;
    priority!: IssuePriority;
    estimate!: string;
    index!: number;
    parentId: number | undefined;
    parentKey: string | undefined;
    childIssueKeys: string[] = [];
    authorId!: number;
    assigneeId!: number;
    projectId!: number;
    timeTrack!: string;
    authorName!: string;
    assigneeName: string | undefined;
}
