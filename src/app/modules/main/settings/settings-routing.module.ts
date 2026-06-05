import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { ChangePasswordComponent } from './components/change-password/change-password.component';
import { DeleteAccountComponent } from './components/delete-account/delete-account.component';
import { EditProfileComponent } from './components/edit-profile/edit-profile.component';
import { SettingsShellComponent } from './components/settings-shell.component';
import { SettingsPage } from './settings.page';

const routes: Routes = [
  {
    children: [
      {
        component: SettingsPage,
        path: '',
      },
      {
        component: EditProfileComponent,
        path: 'edit-profile',
      },
      {
        component: ChangePasswordComponent,
        path: 'change-password',
      },
      {
        component: DeleteAccountComponent,
        path: 'delete-account',
      },
    ],
    component: SettingsShellComponent,
    path: '',
  },
];

@NgModule({
  exports: [RouterModule],
  imports: [RouterModule.forChild(routes)],
})
export class SettingsRoutingModule {}
