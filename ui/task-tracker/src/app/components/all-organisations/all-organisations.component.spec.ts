import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AllOrganisationsComponent } from './all-organisations.component';

describe('AllOrganisationsComponent', () => {
  let component: AllOrganisationsComponent;
  let fixture: ComponentFixture<AllOrganisationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AllOrganisationsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AllOrganisationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
