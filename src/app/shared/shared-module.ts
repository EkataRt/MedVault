import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';

import { CalendarComponent } from './components/calendar/calendar.component';
import { NotificationBellComponent } from './components/notification-bell/notification-bell.component';
import { SearchbarComponent } from './components/searchbar/searchbar.component';
import { ConfirmModalComponent } from './components/confirm-modal/confirm-modal.component';
import { FabButtonComponent } from './components/fab-button/fab-button.component';

@NgModule({
  declarations: [
    CalendarComponent,
    NotificationBellComponent,
    SearchbarComponent,
    ConfirmModalComponent,
    FabButtonComponent,
  ],
  imports: [CommonModule, IonicModule],
  exports: [
    CalendarComponent,
    NotificationBellComponent,
    SearchbarComponent,
    ConfirmModalComponent,
    FabButtonComponent,
  ],
})
export class SharedModule {}
