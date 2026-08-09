import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, forkJoin, Observable, throwError } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import {
  Appointment,
  HealthProfile,
  Medicine,
} from '../../models/med-vault-model';
import {
  AuthResponse,
  LoginRequest,
  SignupRequest,
  User,
} from '../../models/user.model';
import { StorageService } from '../../shared/service/storage/storage-service';

@Injectable({
  providedIn: 'root',
})
export class AuthenticationService {
  private readonly apiUrl = environment.apiUrl;

  private readonly userKey = 'active_user';

  constructor(
    private http: HttpClient,
    private storage: StorageService,
  ) {}

  public deleteUser(userId: string): Observable<void> {
    return forkJoin({
      appointments: this.http.get<Appointment[]>(
        `${this.apiUrl}/appointments?userId=${userId}`,
      ),
      healthProfiles: this.http.get<HealthProfile[]>(
        `${this.apiUrl}/healthProfiles?userId=${userId}`,
      ),
      medicines: this.http.get<Medicine[]>(
        `${this.apiUrl}/medicines?userId=${userId}`,
      ),
    }).pipe(
      switchMap(({ appointments, healthProfiles, medicines }) => {
        const deleteRequests: Observable<void>[] = [
          ...appointments.map((a) =>
            this.http.delete<void>(`${this.apiUrl}/appointments/${a.id}`),
          ),
          ...healthProfiles.map((h) =>
            this.http.delete<void>(`${this.apiUrl}/healthProfiles/${h.id}`),
          ),
          ...medicines.map((m) =>
            this.http.delete<void>(`${this.apiUrl}/medicines/${m.id}`),
          ),
        ];

        if (!deleteRequests.length) {
          return this.http.delete<void>(`${this.apiUrl}/users/${userId}`);
        }

        return forkJoin(deleteRequests).pipe(
          switchMap(() =>
            this.http.delete<void>(`${this.apiUrl}/users/${userId}`),
          ),
        );
      }),
    );
  }

  public getActiveUser(): User | null {
    const raw = this.storage.get(this.userKey);

    if (!raw) return null;

    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }

  public login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<{
        id: string;
        username: string;
        email: string;
        token: string;
      }>(`${this.apiUrl}/users/login`, credentials)
      .pipe(
        map((response) => {
          const { token, ...user } = response;

          this.storage.set(this.userKey, JSON.stringify(user));
          this.storage.set('token', token);

          return { token, user };
        }),
        catchError((error: HttpErrorResponse) => {
          const message: string =
            error.error?.message ?? 'Login failed. Please try again.';

          return throwError((): Error => new Error(message));
        }),
      );
  }

  public logout(): void {
    this.storage.remove(this.userKey);
  }

  public signup(data: SignupRequest): Observable<AuthResponse> {
    return this.http
      .get<User[]>(`${this.apiUrl}/users?email=${data.email}`)
      .pipe(
        switchMap((users) => {
          if (users.length) {
            throw new Error('User already exists');
          }

          return this.http.post<User>(`${this.apiUrl}/users`, {
            email: data.email,
            password: data.password,
            username: data.username,
          });
        }),
        map((user) => {
          const token = `token-${user.id}-${Date.now()}`;
          this.storage.set(this.userKey, JSON.stringify(user));

          return { token, user };
        }),
      );
  }

  public updateUser(userId: string, data: Partial<User>): Observable<User> {
    return this.http.patch<User>(`${this.apiUrl}/users/${userId}`, data).pipe(
      map((updatedUser) => {
        const current = this.getActiveUser();

        if (current?.id === userId) {
          this.storage.set(this.userKey, JSON.stringify(updatedUser));
        }

        return updatedUser;
      }),
    );
  }
}
