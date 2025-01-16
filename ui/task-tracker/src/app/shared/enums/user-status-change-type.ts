export enum UserStatusChangeType {
    All = -1,

    /** С этим статусом создаются запросы */
    Default = 0,

    UserConfirmed = 1,

    UserDeclined = 2,
}