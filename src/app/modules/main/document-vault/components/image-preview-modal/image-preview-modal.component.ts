import { Component, input, output } from '@angular/core';

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

  public formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }
}
