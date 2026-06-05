import { Component, inject, input, output } from '@angular/core';
import { AlertController } from '@ionic/angular';

import { MedDocument } from '../../../../../models/med-vault-model';

@Component({
  selector: 'app-document-card',
  standalone: false,
  templateUrl: './document-card.component.html',
  styleUrl: './document-card.component.scss',
})
export class DocumentCardComponent {
  private readonly alertController = inject(AlertController);

  public readonly document = input.required<MedDocument>();

  public readonly documentClick = output<void>();
  public readonly renameDocument = output<string>();
  public readonly deleteDocument = output<void>();

  public onCardClick(): void {
    this.documentClick.emit();
  }

  public onDelete(): void {
    this.deleteDocument.emit();
  }

  public async onRename(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Rename Document',
      inputs: [
        {
          name: 'name',
          placeholder: 'Document name',
          type: 'text',
          value: this.document().name,
        },
      ],
      buttons: [
        {
          role: 'cancel',
          text: 'Cancel',
        },
        {
          handler: (data: { name: string }) => {
            const trimmed = data.name?.trim();
            if (trimmed) this.renameDocument.emit(trimmed);
          },
          text: 'Rename',
        },
      ],
    });

    await alert.present();
  }

  public formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }
}
