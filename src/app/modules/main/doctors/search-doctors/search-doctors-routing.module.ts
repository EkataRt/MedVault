import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { SearchDoctorsPage } from './search-doctors.page';

const routes: Routes = [
  {
    path: '',
    component: SearchDoctorsPage
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class SearchDoctorsPageRoutingModule {}
