import { FormGroup, UntypedFormGroup } from '@angular/forms';
import { NgbModalRef, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { environment } from '../../../environments/environment';
import { ConfirmModalComponent } from '../components/modals/confirm/confirm.modal.component';
import { ModalInfoModel } from '../model/onlyFrontModels/modalInfo.model';
import { LoaderComponent } from '../components/loader/loader.component';
import { ValidationUtils } from '../utils/validationUtils';
import { TranslateService } from '@ngx-translate/core';

const apiUrl = environment.apiUrl;

export abstract class BaseComponent {
  public promocodeValuePattern = /^[A-Z0-9]{1,10}$/;
  public confirmCodePattern = '[0-9]{6}';
  public confirm2FAPattern = /^[0-9]{6}$/;
  public floatNumberPattern = /^-?(0|[1-9]\d*)([.,]\d+)?$/;
  public letterPattern = /^[äöüÄÖÜßáéúőóüöíÁÉÚŐÓÜÖÍa-zA-Zа-яёА-Я-\s]+$/;
  public emailPattern = /^[_a-z0-9-\+-]+(\.[_a-z0-9-]+)*@[a-z0-9-]+(\.[a-z0-9-]+)*(\.[a-z]{2,})$/i;
  public namePattern = /^[A-ZА-ЯЁ]+([- ][A-ZА-ЯЁ]+)*$/i; // todo: переделать чтобы нельзя было писать aBOba
  public latinPattern = /^[A-Z]+([- ][A-Z]+)*$/i;
  public companyNamePattern = /^[äöüÄÖÜßáéúőóüöíÁÉÚŐÓÜÖÍa-zA-Zа-яёЁА-Я0-9" ,.'-]+$/i;
  public latinAndNumberPattern = /^[A-Za-z0-9]+$/;
  public flightIdPattern = /^[A-Za-z0-9]+$/;
  public filePattern = /\.([a-zA-Z]+)$/;
  public URLPattern =
    /^https?:\/\/(?:www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b(?:[-a-zA-Z0-9()@:%_\+.~#?&\/=]*)$/;
  public phonePattern = /^(?:[+]?\d+(?:\s?\(\d+\))?|\d{3}(?:\s?\(\d+\))?)(?:[-\s]?\d+)+$/;
  public numberPattern = /^[0-9]+$/;
  public timeTrackPattern = /^([0-9]+h\s?)?([0-9]+m\s?)?([0-9]+s\s?)?$/;

  //с помощью этого создается модалка
  private modalRefBase: NgbModalRef | undefined;

  constructor(
    private modalServiceBase: NgbModal,
    private translateService: TranslateService
  ) {}

  public elemIsInvalid(elem: any): boolean {
    return elem.dirty && !elem.untouched && elem.invalid; //pristine еще есть он как дирти вроде
  }

  public markFormGroupTouchedAndDirty(formGroup: UntypedFormGroup) {
    (<any>Object).values(formGroup.controls).forEach((control: UntypedFormGroup) => {
      control.markAsTouched();
      control.markAsDirty();
      control.updateValueAndValidity();

      if (control.controls) {
        this.markFormGroupTouchedAndDirty(control);
      }
    });
  }

  public markFormGroupUnTouchedAndPristine(formGroup: UntypedFormGroup) {
    (<any>Object).values(formGroup.controls).forEach((control: UntypedFormGroup) => {
      control.markAsPristine();
      control.markAsUntouched();
      control.updateValueAndValidity();

      if (control.controls) {
        this.markFormGroupUnTouchedAndPristine(control);
      }
    });
  }

  protected showResponseError(response: any) {
    //если к чему то не можем обратиться то уходим
    if (!response || !response.error) {
      return;
    }
    if (!!response.error.errors && response.error.errors.length != 0) {
      for (let e of response.error.errors) {
        if (e.message == 'Chart loading error') continue;

        return this.showError(e.message);
      }
    } else if (!!response.error.error && !!response.error.error_description) {
      return this.showError(response.error.error_description);
    }
    return;
  }

  public shellConversionString(num: string) {
    if ((num !== null || num != undefined) && !Number.isNaN(parseFloat(num)))
      return parseFloat(num).toLocaleString('ru-RU').replace(',', '.');
    return 0;
  }

  //метод который включает отображение модалки
  protected showModal(modalInfo: ModalInfoModel): Promise<any> {
    var t = this;

    t.modalRefBase = t.modalServiceBase.open(ConfirmModalComponent, {
      centered: true,
      size: 'md'
    });

    //информативный блок
    t.modalRefBase.componentInstance.title = modalInfo.title;
    t.modalRefBase.componentInstance.description = modalInfo.description;
    t.modalRefBase.componentInstance.showDescription = modalInfo.showDescription;
    //блок с настройками кнопок
    t.modalRefBase.componentInstance.showConfirmButton = modalInfo.showConfirmButton;
    t.modalRefBase.componentInstance.showDeclineButton = modalInfo.showDeclineButton;
    t.modalRefBase.componentInstance.showErrorButton = modalInfo.showErrorButton;
    t.modalRefBase.componentInstance.buttonConfirm = modalInfo.buttonConfirm;
    t.modalRefBase.componentInstance.buttonDecline = modalInfo.buttonDecline;
    t.modalRefBase.componentInstance.buttonError = modalInfo.buttonError;
    // .then(result => {}) - result - результат нажатия на кнопки (true/false)
    // .catch((reason) => {}) - reason - результат выхода, при нажатии на крестик или мимо модалки (0)
    return t.modalRefBase.result;
  }

  // упрощенное представление модалки с ошибкой
  protected showError(message: string, titleMes: string = 'Error'): Promise<any> {
    var t = this;

    var mes = message;
    var title = titleMes;

    var modalInfo = new ModalInfoModel();
    modalInfo.title = !!title ? t.translateService.instant(title) : mes;
    modalInfo.description = !!title ? t.translateService.instant(mes) : '';
    modalInfo.showConfirmButton = false;
    modalInfo.showDeclineButton = false;
    modalInfo.showErrorButton = true;

    return t.showModal(modalInfo);
  }

  //упрощенное представление модалки с успешным сообщением
  protected showSuccess(message: string, titleMes: string = 'System'): Promise<any> {
    var t = this;

    var mes = t.translateService.instant(message);
    var title = t.translateService.instant(titleMes);

    var modalInfo = new ModalInfoModel();
    modalInfo.title = !!titleMes ? title : mes;
    modalInfo.description = !!titleMes ? mes : '';
    modalInfo.showDeclineButton = false;

    return t.showModal(modalInfo);
  }

  //упрощенное представление модалки с подтверждением действия
  protected showConfirm(
    message: string,
    title: string,
    showDeclineButton: boolean = false,
    confirmButtonText: string = '',
  ): Promise<any> {
    var t = this;

    var modalInfo = new ModalInfoModel();
    //информативный блок
    modalInfo.title = t.translateService.instant(title);
    modalInfo.description = t.translateService.instant(message);
    modalInfo.showDeclineButton = showDeclineButton;
    modalInfo.buttonConfirm = confirmButtonText;
    return t.showModal(modalInfo);
  }

  public downloadFile(fileUrlStr: string) {
    var url = fileUrlStr.split('|');
    const link = document.createElement('a');
    link.setAttribute('target', '_blank');
    link.setAttribute('href', apiUrl + url[0]); //url файла
    link.setAttribute('download', url[1]); //имя файла
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  public GetTheCurrentWeek() {
    var todayDate = new Date();
    var day = todayDate.getDate();
    var year = todayDate.getFullYear();
    var month = todayDate.getMonth();
    var tzoffset = new Date().getTimezoneOffset() * 60000;
    var dateStart = this.toStringFormat(new Date(+new Date(year, month, day - 7) - tzoffset));
    var dateEnd = this.toStringFormat(new Date(+new Date(year, month, day) - tzoffset));
    return { dateStart: dateStart, dateEnd: dateEnd };
  }

  public GetTheCurrentWeekDate() {
    var t = this;
    var dateSrtBE = t.GetTheCurrentWeek();
    var dateStart = new Date(dateSrtBE.dateStart);
    var dateEnd = new Date(dateSrtBE.dateEnd);
    return { dateStart: dateStart, dateEnd: dateEnd };
  }

  public toStringFormat(date: Date) {
    if (!!date) return date.toISOString().split('T')[0];
    else return '';
  }

  getCurrentLocalization() {
    return localStorage?.getItem('localization') ?? 'en';
  }

  public isValid(text: string, pattern: RegExp, isTouched: boolean = false) {
    if (text && pattern) {
      return pattern.test(text);
    }
    return !isTouched;
  }

  public getExtension(source: string, targets: string[]): boolean {
    var hasExt = false;
    targets.forEach((element) => {
      var filename = source.length - element.length;
      hasExt = hasExt || (filename >= 0 && source.indexOf(element, filename) == filename);
    });
    return hasExt;
  }

  public openFileInNewTab(fileUrl: string) {
    window.open(apiUrl + fileUrl, '_blank');
  }

  public paste(input: { value: string; }) {
    navigator.clipboard.readText().then((text) => {
      input.value = text;
    });
  }

  public copyText(text: string) {
    var t = this;
    navigator.clipboard.writeText(text).then(() => {
      t.showSuccess('Copied successfully');
    });
  }

  /*  замена в строке другой строкой
      originalString - строка на вход,
      replacement - на какую строку заменить,
      startIndex - замена начиная с какого индекса,
      charactersToKeep - сколько символов оставить в конце */
  public replaceSubstringByIndex(
    originalString: string,
    replacement: string,
    startIndex: number,
    charactersToKeep: number,
  ) {
    return (
      originalString.substring(0, startIndex) +
      replacement +
      originalString.substring(originalString.length - charactersToKeep)
    );
  }

  public mathRound(value: number, precision = 0) {
    let multiplier = Math.pow(10, precision);
    return Math.round(value * multiplier) / multiplier;
  }

  public setLoading(needLoad: boolean){
    LoaderComponent.setLoading(needLoad);
  }

  public textErrorStr(elem: any, namepattern: RegExp | null = null) {
    if (this.elemIsInvalid(elem)) {
      var customError = Object.getOwnPropertyNames(elem.errors);
      return elem.errors.required ? "errors.required" :
        elem.errors.min != undefined ? this.translateService.instant("errors.min") + ' ' + elem.errors.min.min :
        elem.errors.maxlength != undefined ? (this.translateService.instant("errors.maxLength") + elem.errors.maxlength.requiredLength) :
        elem.errors.minlength != undefined ? (this.translateService.instant("errors.minLength") + elem.errors.minlength.requiredLength) :
        elem.errors.pattern != undefined && namepattern != null && namepattern == this.latinAndNumberPattern ? "errors.onlyLatinAndNum" :
        elem.errors.pattern != undefined && namepattern != null && namepattern == this.emailPattern ? "errors.invalidField" :
        elem.errors.pattern != undefined && namepattern != null && namepattern == this.namePattern ? "errors.invalidField" :
        elem.errors.pattern != undefined && namepattern != null && namepattern == this.timeTrackPattern ? "errors.invalidTimeTrack" :
        elem.errors.pattern != undefined && namepattern != null && namepattern == this.floatNumberPattern ? "errors.inputFloatNumber" :
        elem.errors.email != undefined ? "errors.invalidField" :
        elem.errors.mismatch != undefined ? "errors.mismatch" :
        !!customError && customError.length>0 ? customError[0] :
              "";
    }
    return "";
  }

  public baseDateValidator(formGroup: FormGroup){
    var dateFromForm = formGroup?.value;
    if(!dateFromForm){
      return;
    }
    var date = new Date(dateFromForm.year, dateFromForm.month - 1, dateFromForm.day, 0, 0, 0, 0);
    var isValidDate = ValidationUtils.validateDate(date);
    if(!isValidDate.isValid)
    {
      let obj = { [isValidDate.errorText] : true };
      return obj;
    }
    return;
  }
}
