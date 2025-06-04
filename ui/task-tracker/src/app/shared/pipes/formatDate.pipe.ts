import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'formatDate'
})
export class FormatDatePipe implements PipeTransform {

  transform(value: string, format: string = 'dd.MM.yyyy'): string {
    if (!value) {
      return ''; // Или можно вернуть какое-то значение по умолчанию
    }

    const date = new Date(value);
    if (isNaN(date.getTime())) {
      return 'Invalid Date'; // Или какое-то сообщение об ошибке
    }

    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear().toString();

    // Заменяем плейсхолдеры в формате
    let formattedDate = format.replace('dd', day);
    formattedDate = formattedDate.replace('MM', month);
    formattedDate = formattedDate.replace('yyyy', year);
    formattedDate = formattedDate.replace('yy', String(year).slice(-2)); // Для формата yy (две последние цифры года)

    return formattedDate;
  }
}