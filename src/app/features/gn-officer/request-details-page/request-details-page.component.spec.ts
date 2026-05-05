import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequestDetailsPageComponent } from './request-details-page.component';

describe('RequestDetailsPageComponent', () => {
  let component: RequestDetailsPageComponent;
  let fixture: ComponentFixture<RequestDetailsPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestDetailsPageComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RequestDetailsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
