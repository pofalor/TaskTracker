export class ProjectModel{
    id!: number;
    name!: string;
    description!: string;
    code!: string;
    startDate!: string;
    endDate: string | undefined;
    authorId!: number;
    projectMgrId!: number;
    workSpaceId!: number;
}