import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { HealthProfile } from '../../models/med-vault-model';

@Injectable({
  providedIn: 'root',
})
export class HealthProfileService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  public createProfile(
    data: Omit<HealthProfile, 'id' | 'age'>,
  ): Observable<HealthProfile> {
    return this.http.post<HealthProfile>(`${this.apiUrl}/healthProfiles`, data);
  }

  public getProfileByUserId(
    userId: string,
  ): Observable<HealthProfile | undefined> {
    return this.http
      .get<HealthProfile[]>(`${this.apiUrl}/healthProfiles?userId=${userId}`)
      .pipe(map((profiles) => profiles[0]));
  }

  public updateProfile(
    profileId: string,
    data: Partial<HealthProfile>,
  ): Observable<HealthProfile> {
    return this.http.patch<HealthProfile>(
      `${this.apiUrl}/healthProfiles/${profileId}`,
      data,
    );
  }
}
