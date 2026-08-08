import { HttpClient, HttpParams } from '@angular/common/http'; import { Injectable, signal } from '@angular/core';
import { forkJoin, Observable, switchMap, tap } from 'rxjs';
import { Folder, MedDocument } from '../../models/med-vault-model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DocumentVaultService {
  private readonly base: string = environment.apiUrl;

  public readonly folders = signal<Folder[]>([]);
  public readonly documents = signal<MedDocument[]>([]);

  constructor(private readonly http: HttpClient) {}

  public loadAll(userId: string): Observable<[Folder[], MedDocument[]]> {
    return forkJoin([
      this.http.get<Folder[]>(`${this.base}/folders?userId=${userId}`),
      this.http.get<MedDocument[]>(`${this.base}/documents?userId=${userId}`),
    ]).pipe(
      tap(([folders, documents]) => {
        this.folders.set(folders);
        this.documents.set(documents);
      }),
    );
  }

  public createFolder(data: Omit<Folder, 'id'>): Observable<Folder> {
    return this.http
      .post<Folder>(`${this.base}/folders`, data)
      .pipe(tap((folder) => this.folders.update((f) => [...f, folder])));
  }

  public renameFolder(id: string, name: string): Observable<Folder> {
    return this.http
      .patch<Folder>(`${this.base}/folders/${id}`, { name })
      .pipe(
        tap((updated) =>
          this.folders.update((f) =>
            f.map((folder) =>
              folder.id === id ? { ...folder, ...updated } : folder,
            ),
          ),
        ),
      );
  }

  public deleteFolder(id: string): Observable<unknown> {
    return this.http
      .delete(`${this.base}/folders/${id}`)
      .pipe(
        tap(() =>
          this.folders.update((f) => f.filter((folder) => folder.id !== id)),
        ),
      );
  }

  public uploadFile(file: File): Observable<{ fileName: string; url: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ fileName: string; url: string }>(
      `${this.base}/documents/upload`,
      form,
    );
  }

  public uploadFromBase64(
    base64: string,
    mimeType: string,
    fileName: string,
  ): Observable<{ fileName: string; url: string }> {
    const byteString = atob(base64);
    const ab = new ArrayBuffer(byteString.length);
    const ia = new Uint8Array(ab);

    for (let i = 0; i < byteString.length; i++) {
      ia[i] = byteString.charCodeAt(i);
    }

    const blob = new Blob([ab], { type: mimeType });
    const file = new File([blob], fileName, { type: mimeType });

    return this.uploadFile(file);
  }

  public createDocument(
    data: Omit<MedDocument, 'id'>,
  ): Observable<MedDocument> {
    return this.http
      .post<MedDocument>(`${this.base}/documents`, data)
      .pipe(tap((doc) => this.documents.update((docs) => [...docs, doc])));
  }

  public renameDocument(id: string, name: string): Observable<MedDocument> {
    return this.http
      .patch<MedDocument>(`${this.base}/documents/${id}`, { name })
      .pipe(
        tap((updated) =>
          this.documents.update((docs) =>
            docs.map((d) => (d.id === id ? { ...d, ...updated } : d)),
          ),
        ),
      );
  }
  public searchDocuments(
    userId: string,
    query: string
  ): Observable<MedDocument[]> {

    const params = new HttpParams()
      .set('userId', userId)
      .set('query', query);

    return this.http.get<MedDocument[]>(
      `${this.base}/documents/search`,
      { params }
    );
  }
  
  public deleteDocument(id: string, fileName: string): Observable<unknown> {
    return this.http.delete(`${this.base}/documents/${id}`).pipe(
      // Updated route: matches /documents/upload/{fileName}
      switchMap(() => this.http.delete(`${this.base}/documents/upload/${fileName}`)),
      tap(() =>
        this.documents.update((docs) => docs.filter((d) => d.id !== id)),
      ),
    );
  }
}
