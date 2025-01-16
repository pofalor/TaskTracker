
import { UserStatusChangeType } from '../enums/user-status-change-type'

export class UserWspStatusChangeModel {
    public id!: number;

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

    /** Эмейл того, кто приглашает в воркспейс */
    public inviterEmail!: string;

    /** Фамилия, имя либо никнейм директора воркспейса */
    public directorWspName!: string;

    /** Фамилия, имя либо никнейм юзера, которого приглашают */
    public userName!: string;

    /** Эмейл юзера, которого приглашают */
    public userEmail!: string;
}