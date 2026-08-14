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

  // Uses reportDate if available, falling back to createdAt
  public displayDate(): string {
    const doc = this.document();
    const targetDate = doc.reportDate || doc.createdAt;
    return this.formatDate(targetDate);
  }

  public formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '';

    const parsedDate = new Date(dateStr);
    if (isNaN(parsedDate.getTime())) return '';

    return parsedDate.toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  // Returns badge color for status rendering
  public getStatusColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scanned':
        return 'success';
      case 'unscanned':
        return 'danger';
      case 'processing':
        return 'warning';
      default:
        return 'medium'; // Pending / Default
    }
  }

  public isPdf(): boolean {
    const url = this.document().url?.toLowerCase() || '';
    const fileName = this.document().fileName?.toLowerCase() || '';
    return url.endsWith('.pdf') || fileName.endsWith('.pdf');
  }
}
