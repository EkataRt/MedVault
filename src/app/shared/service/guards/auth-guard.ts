import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';

import { StorageService } from '../storage/storage-service';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  constructor(
    private router: Router,
    private storageService: StorageService,
  ) {}

  canActivate(): Observable<boolean> | boolean {
    const syncUser = this.storageService.get('active_user');

    if (syncUser) return true;

    return this.storageService.getAsync('active_user').pipe(
      tap((value) => {
        if (value) {
          localStorage.setItem('active_user', value);
        }
      }),
      map((value) => {
        if (value) return true;
        this.router.navigate(['/home/login']);
        return false;
      }),
    );
  }
}
