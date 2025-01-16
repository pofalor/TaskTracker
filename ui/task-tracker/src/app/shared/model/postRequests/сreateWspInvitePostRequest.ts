import { UserWorkSpaceStatus } from "../../enums/user-workspace-status";

export class CreateWspInvitePostRequest {
    workSpaceId!: number;

    userId!: number;

    inviterId : number | undefined;

    /** Дата, когда был создан запрос в UTC */
    date!: string;

    newStatus!: UserWorkSpaceStatus;
}
