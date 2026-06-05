import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-fab-button',
  standalone: false,
  templateUrl: './fab-button.component.html',
  styleUrl: './fab-button.component.scss',
})
export class FabButtonComponent {
  public readonly icon = input.required<string>();
  public readonly fabClick = output<void>();

  public onClick(): void {
    this.fabClick.emit();
  }
}
