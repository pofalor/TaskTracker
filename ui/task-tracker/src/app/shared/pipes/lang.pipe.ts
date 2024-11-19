import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'lang',
  standalone: true
})
export class LangPipe implements PipeTransform {

  transform(value: unknown, ...args: unknown[]): unknown {
    return null;
  }

}
