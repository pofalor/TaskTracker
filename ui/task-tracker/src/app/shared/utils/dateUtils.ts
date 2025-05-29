export class DateUtils {
    /**
     * Получить текущую дату в формате yyyy-mm-dd
     * @param withTime Указывает должно ли содержаться время в дате. Формат времени: hh:mm:ss
     */
    public static getNowDateStr(withTime: boolean = false): string {
        var dateArr = new Date().toISOString().split('T');
        var date = dateArr[0];
        if (!withTime)
            return date;
        var timeWithTicks = dateArr[1];
        var time = timeWithTicks.substring(0, timeWithTicks.indexOf('.'));
        return date + ' ' + time;
    }

    public static getNowUtc() {
        const now = new Date();
        return new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), now.getUTCHours(), now.getUTCMinutes(), now.getUTCSeconds());
    }

    public static timeSince(dateString: string): string {
        // 1. Преобразуем строку даты в объект Date (UTC).  Важно указать часовой пояс 'UTC', чтобы избежать смещений, если dateString не содержит информации о временной зоне.
        const pastDate = new Date(dateString);

        // 2. Получаем текущее время в UTC.
        const now = this.getNowUtc();

        // 3. Вычисляем разницу в миллисекундах.
        const differenceInMilliseconds = now.getTime() - pastDate.getTime();

        // 4. Преобразуем разницу в часы, минуты и секунды.
        const seconds = Math.floor(differenceInMilliseconds / 1000) % 60;
        const minutes = Math.floor(differenceInMilliseconds / (1000 * 60)) % 60;
        const hours = Math.floor(differenceInMilliseconds / (1000 * 60 * 60));

        // 5. Форматируем вывод в HH:mm:ss.  Используем padStart для добавления ведущих нулей.
        const formattedHours = String(hours).padStart(2, '0');
        const formattedMinutes = String(minutes).padStart(2, '0');
        const formattedSeconds = String(seconds).padStart(2, '0');

        return `${formattedHours}:${formattedMinutes}:${formattedSeconds}`;
    }
}