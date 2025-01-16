import { UserStatusChangeType } from "../../enums/user-status-change-type";

export class AcceptInvitePR {
    public id! : number;
    public requestStatus! : UserStatusChangeType;
    public userId : number | undefined;
}