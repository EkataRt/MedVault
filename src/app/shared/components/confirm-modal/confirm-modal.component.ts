import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-modal',
  standalone: false,
  templateUrl: './confirm-modal.component.html',
  styleUrl: './confirm-modal.component.scss',
})
export class ConfirmModalComponent {
  public readonly title = input.required<string>();
  public readonly message = input.required<string>();
  public readonly confirmLabel = input<string>('Confirm');
  public readonly isDanger = input<boolean>(true);
  public readonly isOpen = input.required<boolean>();

  public readonly confirmed = output<void>();
  public readonly cancelled = output<void>();

  public onConfirm(): void {
    this.confirmed.emit();
  }

  public onCancel(): void {
    this.cancelled.emit();
  }
}
