import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { IonicModule } from '@ionic/angular';

import { BmiHistoryPage } from './bmi-history.page';

const routes: Routes = [{ path: '', component: BmiHistoryPage }];

@NgModule({
  declarations: [BmiHistoryPage],
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    RouterModule.forChild(routes),
  ],
})
export class BmiHistoryPageModule { }
