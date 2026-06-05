import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { DashboardPageRoutingModule } from './dashboard-routing.module';
import { DashboardPage } from './dashboard.page';
import { AppointmentListComponent } from './components/appointment-list/appointment-list.component';
import { HealthAnalyticsComponent } from './components/health-analytics/health-analytics.component';
import { MedicineListComponent } from './components/medicine-list/medicine-list.component';
import { UserInfoCardComponent } from './components/user-info-card/user-info-card.component';
import { SharedModule } from '../../../shared/shared-module';
@NgModule({
  imports: [
    CommonModule,
    IonicModule,
    DashboardPageRoutingModule,
    SharedModule,
  ],
  declarations: [
    DashboardPage,
    AppointmentListComponent,
    HealthAnalyticsComponent,
    MedicineListComponent,
    UserInfoCardComponent,
  ],
})
export class DashboardPageModule {}
