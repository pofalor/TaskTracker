export class CreateOrEditProjectPR {
    id: number | undefined;
    name!: string;
    description!: string;
    code!: string;
    startDate!: string;
    endDate: string | undefined;
    authorId: number | undefined;
    projectMgrId!: number;
    workspaceId!: number;
}