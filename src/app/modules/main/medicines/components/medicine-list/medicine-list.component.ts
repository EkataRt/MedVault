import {
  Component,
  DestroyRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import dayjs from 'dayjs';
import { Medicine } from '../../../../../models/med-vault-model';
import { MedicineService } from '../../../../../services/medicine/medicine-service';
import { DataRefreshService } from '../../../../../shared/service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-medicine-list',
  standalone: false,
  templateUrl: './medicine-list.component.html',
  styleUrl: './medicine-list.component.scss',
})
export class MedicineListComponent {
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly medicineService = inject(MedicineService);

  public readonly medicines = input.required<Medicine[]>();
  public readonly medicineDeleted = output<string>();
  public readonly medicineUpdated = output<Medicine>();
  public readonly editRequested = output<Medicine>();

  public readonly deleteTargetId = signal<string | null>(null);
  public readonly errorMessage = signal<string | null>(null);

  public deleteTarget(): Medicine | null {
    const id = this.deleteTargetId();
    if (!id) return null;
    return this.medicines().find((m) => m.id === id) ?? null;
  }

  public formatDateRange(startDate: string, endDate: string): string {
    const start = dayjs(startDate);
    const end = dayjs(endDate);
    const diffDays = end.diff(start, 'day');

    let duration = '';
    if (diffDays < 7) duration = `${diffDays}d`;
    else if (diffDays < 30) duration = `${Math.round(diffDays / 7)}w`;
    else duration = `${Math.round(diffDays / 30)}mo`;

    return `${start.format('MMM D')} – ${end.format('MMM D')} (${duration})`;
  }

  public formatMealPreference(pref: 'after' | 'any' | 'before'): string {
    if (pref === 'before') return 'Before meal';
    if (pref === 'after') return 'After meal';
    return 'Any time';
  }

  public isDoseTakenToday(medicine: Medicine, index: number): boolean {
    return this.medicineService.isDoseTakenToday(medicine, index);
  }

  public onDeleteCancelled(): void {
    this.deleteTargetId.set(null);
  }

  public onDeleteConfirmed(): void {
    const id = this.deleteTargetId();
    if (!id) return;

    this.medicineService
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to delete medicine.'),
        next: () => {
          this.medicineDeleted.emit(id);
          this.deleteTargetId.set(null);
        },
      });
  }

  public onEditRequested(med: Medicine): void {
    this.editRequested.emit(med);
  }

  public openDeleteConfirm(id: string): void {
    this.deleteTargetId.set(id);
  }

  public toggleDoseStatus(medicine: Medicine, doseIndex: number): void {
    const currentlyTaken = this.isDoseTakenToday(medicine, doseIndex);

    this.medicineService
      .updateDoseStatus(
        medicine.id,
        doseIndex,
        !currentlyTaken,
        medicine.lastTakenDates ?? [],
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to update dose status.'),
        next: (result) => {
          this.medicineUpdated.emit(result);
          this.dataRefreshService.emitMedicinesChanged();
        },
      });
  }
}
