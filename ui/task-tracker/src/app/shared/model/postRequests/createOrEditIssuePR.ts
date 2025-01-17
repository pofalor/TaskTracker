import { IssuePriority } from "../../enums/issue-priority";
import { IssueStatus } from "../../enums/issue-status";
import { IssueType } from "../../enums/issue-type";

export class CreateOrEditIssuePR {
    id: number | undefined;
    name!: string;
    description!: string;
    type!: IssueType;
    status!: IssueStatus;
    priority!: IssuePriority;
    estimate: string | undefined;
    index!: number;
    epicId: number | undefined;
    authorId: number | undefined;
    assigneeId: number | undefined;
    projectId!: number;
}