import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AlertController } from '@ionic/angular';
import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';
import { Preferences } from '@capacitor/preferences';
import { map, of, Observable, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Folder, MedDocument } from '../../../models/med-vault-model';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { DocumentVaultService } from '../../../services/document-vault/document-vault-service';
import { environment } from '../../../../environments/environment';
import { SmartSearchResult } from '../../../models/med-vault-model';

type ViewState = 'root' | 'folder' | 'subfolder';

interface BreadcrumbItem {
  id: string | null;
  level: ViewState;
  name: string;
}

@Component({
  selector: 'app-document-vault',
  standalone: false,
  templateUrl: './document-vault.page.html',
  styleUrl: './document-vault.page.scss',
})
export class DocumentVaultPage implements OnInit {
  private readonly vaultService = inject(DocumentVaultService);
  private readonly authService = inject(AuthenticationService);
  private readonly alertController = inject(AlertController);
  private readonly destroyRef = inject(DestroyRef);

  private userId = '';
  private readonly searchSubject = new Subject<string>();

  public readonly view = signal<ViewState>('root');
  public readonly activeFolderId = signal<string | null>(null);
  public readonly isFabOpen = signal<boolean>(false);
  public readonly breadcrumb = signal<BreadcrumbItem[]>([
    { id: null, level: 'root', name: 'My Vault' },
  ]);
  public readonly uploadModalOpen = signal<boolean>(false);
  public readonly previewModalOpen = signal<boolean>(false);
  public readonly activeDocument = signal<MedDocument | null>(null);
  public readonly errorMessage = signal<string | null>(null);
  public readonly cameraFile = signal<File | null>(null);

  // Search state signals
  public readonly searchQuery = signal<string>('');
  public readonly searchResults = signal<MedDocument[]>([]);
  public readonly isSearching = computed(
    () => this.searchQuery().trim().length > 0,
  );

  public readonly smartSearchActive = signal<boolean>(false);
  public readonly smartSearchResults = signal<SmartSearchResult[]>([]);

  public readonly searchPlaceholder = computed(() =>
    this.smartSearchActive()
      ? 'Smart search: e.g. magnesium level from 2 years ago'
      : 'Search documents...',
  );

  ngOnInit(): void {
    const user = this.authService.getActiveUser();
    if (!user?.id) return;

    this.userId = String(user.id);

    this.vaultService
      .loadAll(this.userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.processPendingPhoto());

    // Setup debounced search subscription
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((query) => {
          const isSmartSearch = this.smartSearchActive();

          if (!query.trim()) {
            return of({
              isSmartSearch,
              results: [] as (MedDocument | SmartSearchResult)[],
            });
          }

          const request: Observable<MedDocument[] | SmartSearchResult[]> =
            isSmartSearch
              ? this.vaultService.smartSearchDocuments(this.userId, query)
              : this.vaultService.searchDocuments(this.userId, query);

          return request.pipe(map((results) => ({ isSmartSearch, results })));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ isSmartSearch, results }) => {
          if (isSmartSearch) {
            this.smartSearchResults.set(results as SmartSearchResult[]);
          } else {
            this.searchResults.set(results as MedDocument[]);
          }
        },
        error: () => this.errorMessage.set('Search failed. Please try again.'),
      });
  }

  // --- Search Handler ---
  public onSearch(event: any): void {
    const query = event.detail.value ?? '';
    this.searchQuery.set(query);
    this.searchSubject.next(query);
  }

  public clearSearch(): void {
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  public toggleSmartSearch(): void {
    this.smartSearchActive.update((active) => !active);
    this.searchQuery.set('');
    this.searchResults.set([]);
    this.smartSearchResults.set([]);
  }

  // --- Upload Handler ---
  public onDocumentUploaded(): void {
    this.closeUploadModal();
    // Reload documents list from service to keep data synchronized
    if (this.userId) {
      this.vaultService.loadAll(this.userId).subscribe();
    }
  }

  // --- Signals & Computed Properties ---
  public readonly rootFolders = computed(() =>
    this.vaultService.folders().filter((f) => f.parentId === null),
  );

  public readonly subfolders = computed(() =>
    this.vaultService
      .folders()
      .filter((f) => String(f.parentId) === this.activeFolderId()),
  );

  public readonly activeDocuments = computed(() =>
    this.vaultService
      .documents()
      .filter((d) => String(d.folderId) === this.activeFolderId()),
  );

  public readonly subfolderCount = computed(
    () => (folderId: string) =>
      this.vaultService.folders().filter((f) => String(f.parentId) === folderId)
        .length,
  );

  public readonly fileCount = computed(
    () => (folderId: string) =>
      this.vaultService
        .documents()
        .filter((d) => String(d.folderId) === folderId).length,
  );

  public readonly canCreateFolder = computed(
    () => this.view() === 'root' || this.view() === 'folder',
  );

  public readonly canUpload = computed(
    () => this.view() === 'folder' || this.view() === 'subfolder',
  );

  public toggleFab(): void {
    this.isFabOpen.update((v) => !v);
  }

  public closeFab(): void {
    this.isFabOpen.set(false);
  }

  public onRootFolderClick(folder: Folder): void {
    this.activeFolderId.set(String(folder.id));
    this.view.set('folder');
    this.breadcrumb.set([
      { id: null, level: 'root', name: 'My Vault' },
      { id: String(folder.id), level: 'folder', name: folder.name },
    ]);
  }

  public onSubfolderClick(folder: Folder): void {
    this.activeFolderId.set(String(folder.id));
    this.view.set('subfolder');
    this.breadcrumb.update((b) => [
      ...b,
      { id: String(folder.id), level: 'subfolder', name: folder.name },
    ]);
  }

  public onBreadcrumbClick(item: BreadcrumbItem): void {
    if (item.level === 'root') {
      this.view.set('root');
      this.activeFolderId.set(null);
      this.breadcrumb.set([{ id: null, level: 'root', name: 'My Vault' }]);
    } else if (item.level === 'folder') {
      this.view.set('folder');
      this.activeFolderId.set(item.id);
      this.breadcrumb.update((b) => b.slice(0, 2));
    }
  }

  public onRenameFolder(folder: Folder, newName: string): void {
    this.vaultService
      .renameFolder(folder.id, newName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to rename folder.'),
      });
  }

  public onDeleteFolder(folder: Folder): void {
    this.vaultService
      .deleteFolder(folder.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to delete folder.'),
      });
  }

  public onRenameDocument(doc: MedDocument, newName: string): void {
    this.vaultService
      .renameDocument(doc.id, newName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to rename document.'),
      });
  }

  public onDeleteDocument(doc: MedDocument): void {
    this.vaultService
      .deleteDocument(doc.id, doc.fileName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to delete document.'),
      });
  }

  public openUploadModal(): void {
    const folderId = this.activeFolderId();
    if (!folderId) return;

    this.isFabOpen.set(false);
    this.cameraFile.set(null);
    this.uploadModalOpen.set(true);
  }

  public async openCamera(): Promise<void> {
    const folderId = this.activeFolderId();
    if (!folderId) return;

    this.isFabOpen.set(false);

    await Preferences.set({
      key: 'pending_folder_id',
      value: String(folderId),
    });
    await Preferences.set({ key: 'pending_view', value: this.view() });

    const currentBreadcrumb = this.breadcrumb();
    const folderName =
      currentBreadcrumb[currentBreadcrumb.length - 1]?.name ?? '';
    await Preferences.set({ key: 'pending_folder_name', value: folderName });

    try {
      const photo = await Camera.getPhoto({
        resultType: CameraResultType.Base64,
        source: CameraSource.Camera,
        quality: 85,
      });

      if (!photo.base64String) return;

      const format = photo.format ?? 'jpeg';
      const mimeType = `image/${format}`;
      const fileName = `photo_${Date.now()}.${format}`;
      const file = this.base64ToFile(photo.base64String, mimeType, fileName);

      this.cameraFile.set(file);
      this.uploadModalOpen.set(true);

      await this.clearPendingPreferences();
    } catch {
      await this.clearPendingPreferences();
    }
  }

  public closeUploadModal(): void {
    this.cameraFile.set(null);
    this.uploadModalOpen.set(false);
  }

  public closePreviewModal(): void {
    this.activeDocument.set(null);
    this.previewModalOpen.set(false);
  }

  public openPreview(doc: MedDocument): void {
    this.activeDocument.set(doc);
    this.previewModalOpen.set(true);
  }

  public onPreviewDeleteRequested(): void {
    const doc = this.activeDocument();
    if (!doc) return;
    this.onDeleteDocument(doc);
  }

  public async openNewFolderAlert(): Promise<void> {
    this.isFabOpen.set(false);

    const parentId = this.view() === 'root' ? null : this.activeFolderId();

    const alert = await this.alertController.create({
      buttons: [
        {
          role: 'cancel',
          text: 'Cancel',
        },
        {
          handler: (data: { name: string }) => {
            const trimmed = data.name?.trim();
            if (!trimmed) return;

            this.vaultService
              .createFolder({
                createdAt: new Date().toISOString(),
                name: trimmed,
                parentId,
                userId: this.userId,
              })
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe({
                error: () => this.errorMessage.set('Failed to create folder.'),
              });
          },
          text: 'Create',
        },
      ],
      header: 'New Folder',
      inputs: [
        {
          name: 'name',
          placeholder: 'Folder name',
          type: 'text',
        },
      ],
    });

    await alert.present();
  }

  private async processPendingPhoto(): Promise<void> {
    const [
      photoResult,
      folderIdResult,
      viewResult,
      folderNameResult,
      formatResult,
    ] = await Promise.all([
      Preferences.get({ key: 'pending_photo_base64' }),
      Preferences.get({ key: 'pending_folder_id' }),
      Preferences.get({ key: 'pending_view' }),
      Preferences.get({ key: 'pending_folder_name' }),
      Preferences.get({ key: 'pending_photo_format' }),
    ]);

    const base64 = photoResult.value;
    const folderId = folderIdResult.value;
    const view = viewResult.value as ViewState | null;
    const folderName = folderNameResult.value;
    const format = formatResult.value ?? 'jpeg';

    if (!base64 || !folderId || !view || !folderName) return;

    const folder = this.vaultService
      .folders()
      .find((f) => String(f.id) === folderId);

    if (!folder) {
      await this.clearPendingPreferences();
      return;
    }

    this.activeFolderId.set(folderId);
    this.view.set(view);

    if (view === 'folder') {
      this.breadcrumb.set([
        { id: null, level: 'root', name: 'My Vault' },
        { id: folderId, level: 'folder', name: folderName },
      ]);
    } else {
      const parentFolder = this.vaultService
        .folders()
        .find((f) => String(f.id) === String(folder.parentId));

      this.breadcrumb.set([
        { id: null, level: 'root', name: 'My Vault' },
        {
          id: parentFolder ? String(parentFolder.id) : null,
          level: 'folder',
          name: parentFolder?.name ?? '',
        },
        { id: folderId, level: 'subfolder', name: folderName },
      ]);
    }

    const mimeType = `image/${format}`;
    const fileName = `photo_${Date.now()}.${format}`;
    const file = this.base64ToFile(base64, mimeType, fileName);

    this.cameraFile.set(file);
    this.uploadModalOpen.set(true);

    await this.clearPendingPreferences();
  }

  private base64ToFile(
    base64: string,
    mimeType: string,
    fileName: string,
  ): File {
    const byteString = atob(base64);
    const ab = new ArrayBuffer(byteString.length);
    const ia = new Uint8Array(ab);

    for (let i = 0; i < byteString.length; i++) {
      ia[i] = byteString.charCodeAt(i);
    }

    const blob = new Blob([ab], { type: mimeType });
    return new File([blob], fileName, { type: mimeType });
  }

  private async clearPendingPreferences(): Promise<void> {
    await Promise.all([
      Preferences.remove({ key: 'pending_photo_base64' }),
      Preferences.remove({ key: 'pending_photo_format' }),
      Preferences.remove({ key: 'pending_folder_id' }),
      Preferences.remove({ key: 'pending_view' }),
      Preferences.remove({ key: 'pending_folder_name' }),
    ]);
  }

  public viewDocument(doc: MedDocument): void {
    if (!doc?.url) return;

    let finalUrl = doc.url;

    if (!finalUrl.startsWith('http://') && !finalUrl.startsWith('https://')) {
      const baseOrigin = new URL(environment.apiUrl).origin;

      const cleanPath = finalUrl.startsWith('/') ? finalUrl : `/${finalUrl}`;
      finalUrl = `${baseOrigin}${cleanPath}`;
    }
    finalUrl = finalUrl.replace(/^(https?)\/\/(?!\/)/, '$1://');

    window.open(finalUrl, '_blank');
  }
}
