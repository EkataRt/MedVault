import { Injectable } from '@angular/core';
import { Preferences } from '@capacitor/preferences';
import { from, Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class StorageService {
  public get(key: string): string | null {
    return localStorage.getItem(key);
  }

  public remove(key: string): void {
    localStorage.removeItem(key);
    Preferences.remove({ key });
  }

  public set(key: string, value: string): void {
    localStorage.setItem(key, value);
    Preferences.set({ key, value });
  }

  public getAsync(key: string): Observable<string | null> {
    return from(Preferences.get({ key })).pipe(
      map((result) => result.value),
      catchError(() => of(localStorage.getItem(key))),
    );
  }
}
