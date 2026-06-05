import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DocumentVaultPage } from './document-vault.page';

describe('DocumentVaultPage', () => {
  let component: DocumentVaultPage;
  let fixture: ComponentFixture<DocumentVaultPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(DocumentVaultPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
