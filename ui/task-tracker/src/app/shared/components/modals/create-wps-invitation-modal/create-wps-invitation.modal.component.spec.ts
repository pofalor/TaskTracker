import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateWpsInvitationModalComponent } from './create-wps-invitation-modal.component';

describe('CreateWpsInvitationModalComponent', () => {
  let component: CreateWpsInvitationModalComponent;
  let fixture: ComponentFixture<CreateWpsInvitationModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateWpsInvitationModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateWpsInvitationModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
