import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class DataRefreshService {
  private readonly _appointmentsChanged$ = new Subject<void>();
  private readonly _medicinesChanged$ = new Subject<void>();
  private readonly _profileChanged$ = new Subject<void>();

  public readonly appointmentsChanged$ =
    this._appointmentsChanged$.asObservable();

  public readonly medicinesChanged$ = this._medicinesChanged$.asObservable();

  public readonly profileChanged$ = this._profileChanged$.asObservable();

  public emitAppointmentsChanged(): void {
    this._appointmentsChanged$.next();
  }

  public emitMedicinesChanged(): void {
    this._medicinesChanged$.next();
  }

  public emitProfileChanged(): void {
    this._profileChanged$.next();
  }
}
