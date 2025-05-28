import { AutoTrackTimeStatus } from "../enums/auto-track-time-status";

export class TimeTrackingModel {
    /**
     * Затраченное время
     */
    timeSpent: string = '';

    /**
     * Дата начала работы
     */
    dateBegin!: string; // Не требует инициализации, так как не может быть null

    /**
     * Комментарий к списанным часам
     */
    comment?: string;

    /**
     * Статус автоматического трекинга времени.
     * Если не задано - значит трекинг выполнен руками
     */
    autoTrackStatus?: AutoTrackTimeStatus;

    /**
     * Юзер, который списал часы
     */
    userId!: number;

    issueId!: number;
}