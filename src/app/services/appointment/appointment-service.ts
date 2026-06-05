import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { Appointment } from '../../models/med-vault-model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AppointmentService {
  private readonly apiUrl: string = `${environment.apiUrl}/appointments`;

  constructor(private readonly http: HttpClient) {}

  public create(appointment: Omit<Appointment, 'id'>): Observable<Appointment> {
    return this.http.post<Appointment>(this.apiUrl, appointment);
  }

  public delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  public getAllByUserId(userId: string): Observable<Appointment[]> {
    return this.http
      .get<Appointment[]>(this.apiUrl)
      .pipe(
        map((appointments) =>
          appointments
            .filter((a) => String(a.userId) === String(userId))
            .sort((a, b) => a.date.localeCompare(b.date)),
        ),
      );
  }

  public getById(id: string): Observable<Appointment> {
    return this.http.get<Appointment>(`${this.apiUrl}/${id}`);
  }

  public getUpcomingByUserId(
    userId: string,
    limit: number = 3,
  ): Observable<Appointment[]> {
    const today = new Date().toISOString().split('T')[0];

    return this.http.get<Appointment[]>(this.apiUrl).pipe(
      map((appointments) =>
        appointments
          .filter((a) => String(a.userId) === String(userId))
          .filter((a) => a.date >= today)
          .sort((a, b) => a.date.localeCompare(b.date))
          .slice(0, limit),
      ),
    );
  }

  public markVisited(id: string, visitedDate: string): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiUrl}/${id}`, {
      visited: true,
      visitedDate,
    });
  }

  public update(
    id: string,
    data: Partial<Appointment>,
  ): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiUrl}/${id}`, data);
  }
}
