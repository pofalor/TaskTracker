
import { UserStatusChangeType } from '../enums/user-status-change-type'

export class UserWspStatusChangeModel {
    /** Юзер, которого приглашают или удаляют из рабочего пространства*/
    public userId!: number;

    /** Юзер, который приглашает или удаляет из рабочего пространства */
    public requestCreatorId!: number;

    /** Дата, когда был создан запрос в UTC */
    public date!: string;

    /** Подтвердил юзер приглашение или нет */
    public requestStatus!: UserStatusChangeType;

    /** Скрыл ли юзер с фронта этот запрос(пока не используется, функционал на будущее) */
    public isHidden!: boolean;

    /** Имя рабочего пространства, куда приглашают юзера */
    public workSpaceName!: string;

    /** Фамилия, имя либо никнейм того, кто приглашает в воркспейс */
    public inviterName!: string;

    /** Фамилия, имя либо никнейм директора воркспейса */
    public directorWpsName!: string;
}