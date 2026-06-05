import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';

import { ChangePasswordComponent } from './components/change-password/change-password.component';
import { DeleteAccountComponent } from './components/delete-account/delete-account.component';
import { EditProfileComponent } from './components/edit-profile/edit-profile.component';
import { SettingsRoutingModule } from './settings-routing.module';
import { SettingsShellComponent } from './components/settings-shell.component';
import { SettingsPage } from './settings.page';

@NgModule({
  declarations: [
    ChangePasswordComponent,
    DeleteAccountComponent,
    EditProfileComponent,
    SettingsPage,
    SettingsShellComponent,
  ],
  imports: [
    CommonModule,
    IonicModule,
    ReactiveFormsModule,
    SettingsRoutingModule,
  ],
})
export class SettingsModule {}
