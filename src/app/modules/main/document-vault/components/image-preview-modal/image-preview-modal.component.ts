import { Component, input, output } from '@angular/core';
import { Browser } from '@capacitor/browser';
import { MedDocument } from '../../../../../models/med-vault-model';

@Component({
  selector: 'app-image-preview-modal',
  standalone: false,
  templateUrl: './image-preview-modal.component.html',
  styleUrl: './image-preview-modal.component.scss',
})
export class ImagePreviewModalComponent {
  public readonly isOpen = input.required<boolean>();
  public readonly document = input<MedDocument | null>(null);

  public readonly modalClosed = output<void>();
  public readonly deleteRequested = output<void>();

  public onDismiss(): void {
    this.modalClosed.emit();
  }

  public onDelete(): void {
    this.deleteRequested.emit();
    this.modalClosed.emit();
  }

  public displayDate(): string {
    const doc = this.document();
    if (!doc) return '';
    return this.formatDate(doc.reportDate || doc.createdAt);
  }

  public formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '';

    const parsed = new Date(dateStr);
    if (isNaN(parsed.getTime())) return '';

    return parsed.toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }

  public getStatusColor(status: string | undefined): string {
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'success';
      case 'failed':
        return 'danger';
      case 'processing':
        return 'warning';
      default:
        return 'medium';
    }
  }

  public isPdf(url: string | null | undefined): boolean {
    if (!url) return false;
    return url.toLowerCase().split('?')[0].endsWith('.pdf');
  }

  public async openPdf(url: string | null | undefined): Promise<void> {
    if (!url) return;
    await Browser.open({ url });
  }
}
