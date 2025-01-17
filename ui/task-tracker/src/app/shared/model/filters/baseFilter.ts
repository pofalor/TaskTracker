export interface BaseFilter {
    search: string;
    beginDateStr: string;
    endDateStr: string;
    beginDate: string | undefined;
    endDate: string | undefined;
    isAdmin: boolean;
    userId: number;
}