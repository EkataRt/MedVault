import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { MainPage } from './main.page';

const routes: Routes = [
  {
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        loadChildren: () =>
          import('./dashboard/dashboard.module').then(
            (m) => m.DashboardPageModule,
          ),
        path: 'dashboard',
      },
      {
        loadChildren: () =>
          import('./appointments/appointments.module').then(
            (m) => m.AppointmentsPageModule,
          ),
        path: 'appointments',
      },
      {
        loadChildren: () =>
          import('./document-vault/document-vault.module').then(
            (m) => m.DocumentVaultPageModule,
          ),
        path: 'document-vault',
      },
      {
        loadChildren: () =>
          import('./medicines/medicines.module').then(
            (m) => m.MedicinesPageModule,
          ),
        path: 'medicines',
      },
      {
  loadChildren: () =>
    import('./doctors/search-doctors/search-doctors.module').then(
      (m) => m.SearchDoctorsPageModule,
    ),
  path: 'doctors/search-doctors',
},
      {
        loadChildren: () =>
          import('./settings/settings.module').then((m) => m.SettingsModule),
        path: 'settings',
      },
    ],
    component: MainPage,
    path: '',
  },
];

@NgModule({
  exports: [RouterModule],
  imports: [RouterModule.forChild(routes)],
})
export class MainPageRoutingModule {}
