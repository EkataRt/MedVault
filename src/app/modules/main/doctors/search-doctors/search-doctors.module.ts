import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IonicModule } from '@ionic/angular';

import { SearchDoctorsPageRoutingModule } from './search-doctors-routing.module';

import { SearchDoctorsPage } from './search-doctors.page';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    SearchDoctorsPageRoutingModule
  ],
  declarations: [SearchDoctorsPage]
})
export class SearchDoctorsPageModule {}
