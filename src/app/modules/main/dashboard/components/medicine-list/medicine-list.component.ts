import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Medicine } from '../../../../../models/med-vault-model';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { MedicineService } from '../../../../../services/medicine/medicine-service';
import { DataRefreshService } from '../../../../../shared/service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-medicine-list',
  templateUrl: './medicine-list.component.html',
  styleUrls: ['./medicine-list.component.scss'],
  standalone: false,
})
export class MedicineListComponent implements OnInit {
  private readonly auth = inject(AuthenticationService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly medicineService = inject(MedicineService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal<boolean>(true);
  protected readonly medicines = signal<Medicine[]>([]);

  ngOnInit(): void {
    const userId = this.auth.getActiveUser()?.id;
    if (!userId) {
      this.isLoading.set(false);
      return;
    }

    this.load(userId);

    this.dataRefreshService.medicinesChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load(userId));
  }

  protected isDoseTakenToday(medicine: Medicine, index: number): boolean {
    return this.medicineService.isDoseTakenToday(medicine, index);
  }

  protected navigateToMedicines(): void {
    this.router.navigate(['/main/medicines']);
  }

  protected toggleDoseStatus(medicine: Medicine, doseIndex: number): void {
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
        next: (updated) => {
          this.medicines.update((list) =>
            list.map((m) => (m.id === updated.id ? updated : m)),
          );
        },
      });
  }

  private load(userId: string): void {
    this.isLoading.set(true);
    this.medicineService
      .getTodaysByUserId(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.isLoading.set(false),
        next: (data) => {
          this.medicines.set(data);
          this.isLoading.set(false);
        },
      });
  }
}
