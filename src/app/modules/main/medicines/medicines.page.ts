import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Medicine } from '../../../models/med-vault-model';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { MedicineService } from '../../../services/medicine/medicine-service';
import { DataRefreshService } from '../../../shared/service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-medicines',
  standalone: false,
  templateUrl: './medicines.page.html',
  styleUrl: './medicines.page.scss',
})
export class MedicinesPage implements OnInit {
  private readonly authService = inject(AuthenticationService);
  private readonly medicineService = inject(MedicineService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly medicines = signal<Medicine[]>([]);
  public readonly formOpen = signal<boolean>(false);
  public readonly editingMedicine = signal<Medicine | null>(null);
  public readonly errorMessage = signal<string | null>(null);

  public readonly filteredMedicines = computed((): Medicine[] =>
    this.medicines(),
  );

  ngOnInit(): void {
    const userId = this.authService.getActiveUser()?.id;
    if (!userId) return;

    this.medicineService
      .getAllByUserId(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to load medicines.'),
        next: (data) => this.medicines.set(data),
      });
  }

  public closeForm(): void {
    this.editingMedicine.set(null);
    this.formOpen.set(false);
  }

  public onFormSubmitted(result: Medicine): void {
    this.medicines.update((list) => {
      const editing = this.editingMedicine();
      if (editing) {
        return list.map((m) => (m.id === editing.id ? result : m));
      }
      return [...list, result];
    });
    this.dataRefreshService.emitMedicinesChanged();
  }

  public onMedicineDeleted(id: string): void {
    this.medicines.update((list) => list.filter((m) => m.id !== id));
    this.dataRefreshService.emitMedicinesChanged();
  }

  public onMedicineUpdated(updated: Medicine): void {
    this.medicines.update((list) =>
      list.map((m) => (m.id === updated.id ? updated : m)),
    );
    this.dataRefreshService.emitMedicinesChanged();
  }

  public openEdit(med: Medicine): void {
    this.editingMedicine.set(med);
    this.formOpen.set(true);
  }

  public openForm(): void {
    this.editingMedicine.set(null);
    this.formOpen.set(true);
  }
}
