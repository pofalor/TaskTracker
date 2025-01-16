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
     * Преобразовать дату в объект(ngbDatepicker) из строки в формате yyyy-mm-dd
     */
    public static dateFromStr(dateStr: string | undefined){
        if(!!dateStr){
            var dateSplited = dateStr.split('-');
            var dateObj = {
                year: +dateSplited[0],
                month: +dateSplited[1],
                day: +dateSplited[2]
            };
            return dateObj;
        }
        return undefined;
    }
}