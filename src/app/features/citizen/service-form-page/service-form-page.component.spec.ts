import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ServiceFormPageComponent } from './service-form-page.component';

describe('ServiceFormPageComponent', () => {
  let component: ServiceFormPageComponent;
  let fixture: ComponentFixture<ServiceFormPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceFormPageComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ServiceFormPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
