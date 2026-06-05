import { TestBed } from '@angular/core/testing';

import { HealthProfileService } from './health-profile-service';

describe('HealthProfileService', () => {
  let service: HealthProfileService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(HealthProfileService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
