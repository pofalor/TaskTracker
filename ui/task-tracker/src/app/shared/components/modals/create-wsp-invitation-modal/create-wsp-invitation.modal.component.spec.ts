import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateWspInvitationModalComponent as CreateWspInvitationModalComponent } from './create-wsp-invitation.modal.component';

describe('CreateWspInvitationModalComponent', () => {
  let component: CreateWspInvitationModalComponent;
  let fixture: ComponentFixture<CreateWspInvitationModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateWspInvitationModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateWspInvitationModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
