import { UserWorkspaceStatus } from "../../enums/user-workspace-status";

export class CreateWspInvitePostRequest {
    workspaceId!: number;

    userId!: number;

    inviterId : number | undefined;

    /** Дата, когда был создан запрос в UTC */
    date!: string;

    newStatus!: UserWorkspaceStatus;
}
