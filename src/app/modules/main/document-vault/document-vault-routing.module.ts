import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { DocumentVaultPage } from './document-vault.page';

const routes: Routes = [
  {
    path: '',
    component: DocumentVaultPage,
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DocumentVaultPageRoutingModule {}
