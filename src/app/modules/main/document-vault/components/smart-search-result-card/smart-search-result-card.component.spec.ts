import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '@ionic/angular';

import { SmartSearchResultCardComponent } from './smart-search-result-card.component';

describe('SmartSearchResultCardComponent', () => {
  let component: SmartSearchResultCardComponent;
  let fixture: ComponentFixture<SmartSearchResultCardComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ SmartSearchResultCardComponent ],
      imports: [IonicModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(SmartSearchResultCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
