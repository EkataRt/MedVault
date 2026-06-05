import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  output,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, Validators } from '@angular/forms';
import dayjs, { ManipulateType } from 'dayjs';

import { Medicine } from '../../../../../models/med-vault-model';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { MedicineService } from '../../../../../services/medicine/medicine-service';

@Component({
  selector: 'app-medicine-form',
  standalone: false,
  templateUrl: './medicine-form.component.html',
  styleUrl: './medicine-form.component.scss',
})
export class MedicineFormComponent {
  private readonly authService = inject(AuthenticationService);
  private readonly medicineService = inject(MedicineService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly isOpen = input.required<boolean>();
  public readonly editingMedicine = input<Medicine | null>(null);

  public readonly formSubmitted = output<Medicine>();
  public readonly modalClosed = output<void>();

  protected useDuration = false;
  protected isSubmitting = false;
  protected errorMessage: string | null = null;

  constructor() {
    effect(() => {
      const editing = this.editingMedicine();

      if (editing) {
        this.useDuration = false;
        this.form.reset({
          condition: editing.condition,
          dosage: editing.dosage,
          durationAmount: '',
          durationUnit: 'months',
          endDate: editing.endDate,
          mealPreference: editing.mealPreference,
          name: editing.name,
          startDate: editing.startDate,
          timesPerDay: editing.timesPerDay,
        });
        this.setTimesArray(editing.times);
      } else {
        this.resetForm();
      }
    });

    this.form.controls.timesPerDay.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((n) => this.rebuildTimesArray(n));
  }

  protected readonly form = new FormGroup({
    condition: new FormControl('', { nonNullable: true }),
    dosage: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    durationAmount: new FormControl('', { nonNullable: true }),
    durationUnit: new FormControl('months', { nonNullable: true }),
    endDate: new FormControl('', { nonNullable: true }),
    mealPreference: new FormControl<'before' | 'after' | 'any'>('any', {
      nonNullable: true,
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    startDate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    times: new FormArray<FormControl<string>>([]),
    timesPerDay: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.min(1)],
    }),
  });

  public readonly modalTitle = computed(() =>
    this.editingMedicine() ? 'Edit Medicine' : 'Add Medicine',
  );

  protected get timesArray(): FormArray<FormControl<string>> {
    return this.form.controls.times;
  }

  protected readonly calculatedEndDate = computed((): string | null => {
    if (!this.useDuration) return null;

    const { durationAmount, durationUnit, startDate } = this.form.getRawValue();

    if (!startDate || !durationAmount) return null;

    const amount = parseInt(durationAmount);

    if (isNaN(amount)) return null;

    return dayjs(startDate)
      .add(amount, durationUnit as ManipulateType)
      .format('YYYY-MM-DD');
  });

  protected setMealPreference(value: 'after' | 'any' | 'before'): void {
    this.form.controls.mealPreference.setValue(value);
  }

  protected setUseDuration(value: boolean): void {
    this.useDuration = value;
  }

  protected onDismiss(): void {
    this.resetForm();
    this.modalClosed.emit();
  }

  protected submitMedicine(): void {
    const userId = this.authService.getActiveUser()?.id;

    if (!userId || this.isSubmitting || this.form.invalid) return;

    this.isSubmitting = true;

    const f = this.form.getRawValue();
    const editing = this.editingMedicine();
    const endDate = this.useDuration
      ? (this.calculatedEndDate() ?? f.endDate)
      : f.endDate;

    const frequencyMap: Record<number, string> = {
      1: 'Daily',
      2: 'Twice Daily',
      3: 'Three Times Daily',
    };

    const lastTakenDates: (string | null)[] = editing
      ? [...(editing.lastTakenDates ?? Array(f.timesPerDay).fill(null))]
      : Array(f.timesPerDay).fill(null);

    const payload: Omit<Medicine, 'id'> = {
      condition: f.condition,
      dosage: f.dosage,
      endDate: endDate || f.startDate,
      frequency: frequencyMap[f.timesPerDay] ?? `${f.timesPerDay}x Daily`,
      lastTakenDates,
      mealPreference: f.mealPreference,
      name: f.name,
      startDate: f.startDate,
      times: f.times,
      timesPerDay: f.timesPerDay,
      userId,
    };

    const request$ = editing
      ? this.medicineService.update(editing.id, payload)
      : this.medicineService.create(payload);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      error: () => {
        this.errorMessage = 'Failed to submit medicine.';
        this.isSubmitting = false;
      },
      next: (result) => {
        this.formSubmitted.emit(result);
        this.isSubmitting = false;
        this.onDismiss();
      },
    });
  }

  private rebuildTimesArray(n: number): void {
    this.setTimesArray(this.medicineService.calculateTimes(n));
  }

  private setTimesArray(times: string[]): void {
    this.timesArray.clear();
    times.forEach((t) =>
      this.timesArray.push(new FormControl(t, { nonNullable: true })),
    );
  }

  private resetForm(): void {
    this.useDuration = false;
    this.errorMessage = null;
    this.form.reset({
      condition: '',
      dosage: '',
      durationAmount: '',
      durationUnit: 'months',
      endDate: '',
      mealPreference: 'any',
      name: '',
      startDate: '',
      timesPerDay: 1,
    });
    this.setTimesArray(this.medicineService.calculateTimes(1));
  }
}
