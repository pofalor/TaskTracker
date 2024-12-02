import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { Component, Input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-confirm',
    imports: [CommonModule],
    templateUrl: './confirm.modal.component.html',
    styleUrl: './confirm.modal.component.scss'
})
export class ConfirmModalComponent {
  @Input() title: string = '';
  @Input() description: string = '';
  @Input() showDescription: boolean = true;
  @Input() showTitle: boolean = true;
  @Input() buttonConfirm: string = '';
  @Input() showConfirmButton: boolean = true;
  @Input() buttonDecline: string = '';
  @Input() showDeclineButton: boolean = true;
  @Input() buttonError: string = '';
  @Input() showErrorButton: boolean = false;
  public descriptionHTML: SafeHtml | undefined;

  constructor(
    public activeModal: NgbActiveModal,
    private sanitizer: DomSanitizer,
  ) {}

  ngOnInit() {
    this.descriptionHTML = this.sanitizer.bypassSecurityTrustHtml(this.description);
    document.addEventListener('touchmove', (e) => e.preventDefault(), { passive: false });
  }

  ngOnDestroy(): void {
    document.removeEventListener('touchmove', (e) => e.preventDefault());
  }

  public isConfirm(resp: boolean) {
    this.activeModal.close(resp);
  }
}
