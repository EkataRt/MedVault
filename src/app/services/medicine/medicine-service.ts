import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Medicine } from '../../models/med-vault-model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class MedicineService {
  private readonly apiUrl: string = `${environment.apiUrl}/medicines`;

  constructor(private readonly http: HttpClient) {}

  public calculateTimes(timesPerDay: number): string[] {
    const interval = Math.floor((24 * 60) / timesPerDay);
    const times: string[] = [];

    for (let i = 0; i < timesPerDay; i++) {
      const totalMinutes = i * interval;
      const hours = Math.floor(totalMinutes / 60);
      const minutes = totalMinutes % 60;
      times.push(
        `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`,
      );
    }

    return times;
  }

  public create(medicine: Omit<Medicine, 'id'>): Observable<Medicine> {
    return this.http.post<Medicine>(this.apiUrl, medicine);
  }

  public delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  public getAllByUserId(userId: string): Observable<Medicine[]> {
    return this.http
      .get<Medicine[]>(this.apiUrl)
      .pipe(
        map((medicines) =>
          medicines.filter((m) => String(m.userId) === String(userId)),
        ),
      );
  }

  public getById(id: string): Observable<Medicine> {
    return this.http.get<Medicine>(`${this.apiUrl}/${id}`);
  }

  public getTodaysByUserId(userId: string): Observable<Medicine[]> {
    const today = this.localDateString();
    return this.http.get<Medicine[]>(this.apiUrl).pipe(
      map((medicines) =>
        medicines
          .filter((m) => String(m.userId) === String(userId))
          .filter((m) => m.startDate <= today && m.endDate >= today)
          .sort((a, b) => {
            const firstTimeA = a.times?.[0] ?? '00:00';
            const firstTimeB = b.times?.[0] ?? '00:00';
            return firstTimeA.localeCompare(firstTimeB);
          }),
      ),
    );
  }

  public isDoseTakenToday(medicine: Medicine, doseIndex: number): boolean {
    const today = this.localDateString();
    const dates = medicine.lastTakenDates ?? [];
    return dates[doseIndex] === today;
  }

  public update(id: string, data: Partial<Medicine>): Observable<Medicine> {
    return this.http.patch<Medicine>(`${this.apiUrl}/${id}`, data);
  }

  public updateDoseStatus(
    id: string,
    doseIndex: number,
    markTaken: boolean,
    currentLastTakenDates: (string | null)[],
  ): Observable<Medicine> {
    const today = this.localDateString();
    const updated = [...currentLastTakenDates];
    updated[doseIndex] = markTaken ? today : null;
    return this.http.patch<Medicine>(`${this.apiUrl}/${id}`, {
      lastTakenDates: updated,
    });
  }

  private localDateString(): string {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
