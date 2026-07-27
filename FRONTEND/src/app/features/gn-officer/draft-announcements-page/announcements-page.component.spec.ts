import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DraftAnnouncementsPageComponent } from './draft-announcements-page.component';

describe('DraftAnnouncementsPageComponent', () => {
  let component: DraftAnnouncementsPageComponent;
  let fixture: ComponentFixture<DraftAnnouncementsPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DraftAnnouncementsPageComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DraftAnnouncementsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
