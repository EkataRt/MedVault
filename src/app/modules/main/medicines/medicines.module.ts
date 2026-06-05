import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';

import { MedicinesPageRoutingModule } from './medicines-routing.module';
import { MedicinesPage } from './medicines.page';
import { MedicineFormComponent } from './components/medicine-form/medicine-form.component';
import { MedicineListComponent } from './components/medicine-list/medicine-list.component';
import { SharedModule } from '../../../shared/shared-module';
import { ReactiveFormsModule } from '@angular/forms';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    MedicinesPageRoutingModule,
    SharedModule,
    ReactiveFormsModule
  ],
  declarations: [MedicinesPage, MedicineFormComponent, MedicineListComponent],
})
export class MedicinesPageModule {}
