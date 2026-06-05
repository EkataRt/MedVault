import { TestBed } from '@angular/core/testing';

import { DocumentVaultService } from './document-vault-service';

describe('DocumentVaultService', () => {
  let service: DocumentVaultService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DocumentVaultService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
