import { Injectable, Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({ name: 'lang', pure: false })
export class LangPipe implements PipeTransform {

  private variableRegEx = /\{(.+?)\}/g;

  constructor(private translate: TranslateService) {}

  // {{'some text'|lang:[some variables]}}
  // text example: "Text to translate to {Lang} language" - '{Lang}' - variable to replace

  transform(text: string, replaces: string[] | null = null) {

    if (!text || text.length == 0)
      return text;
    
    var t = this;
    var translated = t.translate.instant(text);
    if(translated.match(t.variableRegEx) != null && replaces != null){
      var replaceMap: { key: any; value: string; }[] = [];
      translated.match(t.variableRegEx).forEach((result: any) => {
        replaceMap.push({key: result, value: replaces[replaceMap.length]});
      });
      replaceMap.forEach(map =>{
        translated = translated.replace(map.key, map.value);
      });
    }
    
    return translated;
  }
}