import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WorkspaceInfoComponent } from './workspace-info.component';

describe('WorkspaceInfoComponent', () => {
  let component: WorkspaceInfoComponent;
  let fixture: ComponentFixture<WorkspaceInfoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceInfoComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WorkspaceInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
