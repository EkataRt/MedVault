import { Component, input, output, signal, inject } from '@angular/core';
import { AlertController } from '@ionic/angular';

import { Folder } from '../../../../../models/med-vault-model';

@Component({
  selector: 'app-folder-card',
  standalone: false,
  templateUrl: './folder-card.component.html',
  styleUrl: './folder-card.component.scss',
})
export class FolderCardComponent {
  private readonly alertController = inject(AlertController);

  public readonly folder = input.required<Folder>();
  public readonly subfolderCount = input<number>(0);
  public readonly fileCount = input<number>(0);

  public readonly folderClick = output<void>();
  public readonly renameFolder = output<string>();
  public readonly deleteFolder = output<void>();

  public readonly deleteTargetId = signal<string | null>(null);

  public onCardClick(): void {
    this.folderClick.emit();
  }

  public onDelete(): void {
    this.deleteFolder.emit();
  }

  public async onRename(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Rename Folder',
      inputs: [
        {
          name: 'name',
          placeholder: 'Folder name',
          type: 'text',
          value: this.folder().name,
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
            if (trimmed) this.renameFolder.emit(trimmed);
          },
          text: 'Rename',
        },
      ],
    });

    await alert.present();
  }
}
