import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SearchDoctorsPage } from './search-doctors.page';

describe('SearchDoctorsPage', () => {
  let component: SearchDoctorsPage;
  let fixture: ComponentFixture<SearchDoctorsPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(SearchDoctorsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
