import {
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { switchMap } from 'rxjs';

import { MedDocument } from '../../../../../models/med-vault-model';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { DocumentVaultService } from '../../../../../services/document-vault/document-vault-service';

@Component({
  selector: 'app-upload-modal',
  standalone: false,
  templateUrl: './upload-modal.component.html',
  styleUrl: './upload-modal.component.scss',
})
export class UploadModalComponent {
  private readonly vaultService = inject(DocumentVaultService);
  private readonly authService = inject(AuthenticationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);

  public readonly isOpen = input.required<boolean>();
  public readonly folderId = input.required<string>();
  public readonly preloadedFile = input<File | null>(null);

  public readonly documentUploaded = output<MedDocument>();
  public readonly modalClosed = output<void>();

  public readonly previewUrl = signal<string | null>(null);
  public readonly selectedFile = signal<File | null>(null);
  public readonly documentName = signal<string>('');
  public readonly isUploading = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);

  constructor() {
    effect(() => {
      const open = this.isOpen();
      const file = this.preloadedFile();

      if (!open || !file) return;

      this.selectedFile.set(file);
      this.documentName.set(file.name.replace(/\.[^/.]+$/, ''));

      const reader = new FileReader();
      reader.onload = (e) => this.previewUrl.set(e.target?.result as string);
      reader.readAsDataURL(file);
    });
  }

  public onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    this.selectedFile.set(file);
    this.documentName.set(file.name.replace(/\.[^/.]+$/, ''));

    const reader = new FileReader();
    reader.onload = (e) => this.previewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  public onNameChange(value: string): void {
    this.documentName.set(value);
  }

  public onDismiss(): void {
    this.resetForm();
    this.modalClosed.emit();
  }

  public submit(): void {
    const file = this.selectedFile();
    const name = this.documentName().trim();
    const userId = this.authService.getActiveUser()?.id;
    const folderId = this.folderId();

    if (!file || !name || !userId || !folderId || this.isUploading()) return;

    this.isUploading.set(true);

    this.vaultService
      .uploadFile(file)
      .pipe(
        switchMap(({ fileName, url }) =>
          this.vaultService.createDocument({
            createdAt: new Date().toISOString(),
            fileName,
            folderId,
            name,
            url,
            userId,
          }),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () => {
          this.errorMessage.set('Failed to upload document.');
          this.isUploading.set(false);
        },
        next: (doc) => {
          this.documentUploaded.emit(doc);
          this.isUploading.set(false);
          this.onDismiss();
        },
      });
  }

  // Detect whether the selected file / preview URL points to a PDF file
  public isPdf(fileOrUrl: File | string | null | undefined): boolean {
    if (!fileOrUrl) return false;

    if (typeof fileOrUrl === 'string') {
      return fileOrUrl.toLowerCase().split('?')[0].endsWith('.pdf');
    }

    return (
      fileOrUrl.type === 'application/pdf' ||
      fileOrUrl.name.toLowerCase().endsWith('.pdf')
    );
  }

  // Sanitize the URL so Angular allows it inside an iframe src
  public safeUrl(url: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  private resetForm(): void {
    this.previewUrl.set(null);
    this.selectedFile.set(null);
    this.documentName.set('');
    this.errorMessage.set(null);
    this.isUploading.set(false);
  }
}
