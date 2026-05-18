import { IssuePriority } from "../../enums/issue-priority";
import { IssueStatus } from "../../enums/issue-status";
import { IssueType } from "../../enums/issue-type";

export class IssueEstimatePredictionPR {
    id: number | undefined;
    name!: string;
    description!: string;
    type!: IssueType;
    status!: IssueStatus;
    priority!: IssuePriority;
    parentId: number | undefined;
    assigneeId: number | undefined;
    projectId!: number;
}
