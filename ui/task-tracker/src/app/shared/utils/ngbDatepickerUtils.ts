export class DatepickerUtils {

    /**
     * Преобразовать дату из объекта, который предоставляет ngbDatepicker, в строку в формате yyyy-mm-dd
     */
    public static dateToStr(dateObj: any): string {
        //3, чтобы локальное время перевелось в ISO
        var t = this;
        var val = t.dateObjToStr(dateObj.year, dateObj.month, dateObj.day);
        var dayInISO = +val.split('-')[2];
        if (dayInISO < dateObj.day) {
            val = t.dateObjToStr(dateObj.year, dateObj.month, dateObj.day + 1);
        }
        return val;
    }

    public static dateObjToStr(year: number, month: number, day: number): string {
        var date = new Date(year, month - 1, day);
        return date.toISOString().split('T')[0];
    }

    /**
     * Преобразовать дату в объект(ngbDatepicker) из строки в формате yyyy-mm-dd или dd.mm.yyyy
     */
    public static dateFromStr(dateStr: string | undefined){
        if(!!dateStr){
            var determiner = dateStr.includes('.') ? '.' : dateStr.includes('-') ? '-' : '';
            var dateSplited = dateStr.split(determiner);
            var yearIndex = determiner == '-' ? 0 : 2;
            var monthIndex = 1;
            var dayIndex =  determiner == '-' ? 2 : 0;
            var dateObj = {
                year: +dateSplited[yearIndex],
                month: +dateSplited[monthIndex],
                day: +dateSplited[dayIndex]
            };
            return dateObj;
        }
        return undefined;
    }
}