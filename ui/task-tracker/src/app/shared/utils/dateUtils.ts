export class DateUtils {
    /**
     * Получить текущую дату в формате yyyy-mm-dd
     * @param withTime Указывает должно ли содержаться время в дате. Формат времени: hh:mm:ss
     */
    public static getNowDateStr(withTime: boolean = false): string {
        var dateArr = new Date().toISOString().split('T');
        var date = dateArr[0];
        if(!withTime)
            return date;
        var timeWithTicks = dateArr[1];
        var time = timeWithTicks.substring(0, timeWithTicks.indexOf('.'));
        return date + ' ' + time;
    }
}