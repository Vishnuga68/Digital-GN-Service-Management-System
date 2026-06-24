import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentVaultPageComponent } from './document-vault-page.component';

describe('DocumentVaultPageComponent', () => {
  let component: DocumentVaultPageComponent;
  let fixture: ComponentFixture<DocumentVaultPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentVaultPageComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentVaultPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
