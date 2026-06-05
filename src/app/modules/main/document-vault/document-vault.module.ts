import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { DocumentVaultPageRoutingModule } from './document-vault-routing.module';
import { DocumentVaultPage } from './document-vault.page';
import { DocumentCardComponent } from './components/document-card/document-card.component';
import { FolderCardComponent } from './components/folder-card/folder-card.component';
import { ImagePreviewModalComponent } from './components/image-preview-modal/image-preview-modal.component';
import { UploadModalComponent } from './components/upload-modal/upload-modal.component';
import { SharedModule } from '../../../shared/shared-module';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    DocumentVaultPageRoutingModule,
    SharedModule,
  ],
  declarations: [
    DocumentVaultPage,
    DocumentCardComponent,
    FolderCardComponent,
    ImagePreviewModalComponent,
    UploadModalComponent,
  ],
})
export class DocumentVaultPageModule {}
