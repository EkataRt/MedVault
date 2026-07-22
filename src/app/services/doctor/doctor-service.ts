import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Doctor } from '../../models/med-vault-model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DoctorService {
  private readonly apiUrl: string = `${environment.apiUrl}/doctor`;

  constructor(private readonly http: HttpClient) { }

  public getAvailableLocations(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/locations`);
  }

  public searchNearby(
    location: string,
    healthProblem: string,
    hospitalType: 'all' | 'private' | 'public',
    isSort: boolean,
  ): Observable<Doctor[]> {
    let params = new HttpParams()
      .set('location', location)
      .set('isSort', isSort.toString());

    if (healthProblem) {
      params = params.set('healthProblem', healthProblem);
    }

    if (hospitalType !== 'all') {
      params = params.set('hospitalType', hospitalType);
    }

    return this.http.get<Doctor[]>(`${this.apiUrl}/search`, { params });
  }
}
