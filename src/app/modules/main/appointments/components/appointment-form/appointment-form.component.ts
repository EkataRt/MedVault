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
import { FormControl, FormGroup, Validators } from '@angular/forms';
import dayjs, { ManipulateType } from 'dayjs';

import { Appointment } from '../../../../../models/med-vault-model';
import { AppointmentService } from '../../../../../services/appointment/appointment-service';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';

type AppointmentType = 'new' | 'followup';

@Component({
  selector: 'app-appointment-form',
  standalone: false,
  templateUrl: './appointment-form.component.html',
  styleUrl: './appointment-form.component.scss',
})
export class AppointmentFormComponent {
  private readonly appointmentService = inject(AppointmentService);
  private readonly authService = inject(AuthenticationService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly isOpen = input.required<boolean>();
  public readonly editingAppointment = input<Appointment | null>(null);
  public readonly appointments = input.required<Appointment[]>();

  public readonly formSubmitted = output<Appointment>();
  public readonly modalClosed = output<void>();

  protected appointmentType: AppointmentType = 'new';
  protected isSubmitting = false;
  protected errorMessage: string | null = null;

  constructor() {
    effect(() => {
      const editing = this.editingAppointment();

      if (editing) {
        this.appointmentType = editing.isFollowUp ? 'followup' : 'new';
        this.newForm.reset({
          category: editing.category,
          date: editing.date,
          doctor: editing.doctor,
          hospital: editing.hospital,
          location: editing.location ?? '',
          time: editing.time,
          title: editing.title,
        });
        this.followUpForm.reset({
          date: editing.date,
          doctor: editing.doctor,
          hospital: editing.hospital,
          interval: editing.followUpInterval ?? '',
          location: editing.location ?? '',
          previousAppointmentId: editing.previousAppointmentId ?? '',
          time: editing.time,
          title: editing.title,
        });
      } else {
        this.resetForms();
      }
    });

    this.followUpForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.calculateFollowUpDate());
  }

  protected readonly newForm = new FormGroup({
    category: new FormControl('', { nonNullable: true }),
    date: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    doctor: new FormControl('', { nonNullable: true }),
    hospital: new FormControl('', { nonNullable: true }),
    location: new FormControl('', { nonNullable: true }),
    time: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  protected readonly followUpForm = new FormGroup({
    category: new FormControl('', { nonNullable: true }),
    date: new FormControl('', { nonNullable: true }),
    doctor: new FormControl('', { nonNullable: true }),
    hospital: new FormControl('', { nonNullable: true }),
    interval: new FormControl('', { nonNullable: true }),
    location: new FormControl('', { nonNullable: true }),
    previousAppointmentId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    time: new FormControl('', { nonNullable: true }),
    title: new FormControl('', { nonNullable: true }),
  });

  public readonly modalTitle = computed(() =>
    this.editingAppointment() ? 'Edit Appointment' : 'Add Appointment',
  );

  protected setAppointmentType(type: AppointmentType): void {
    this.appointmentType = type;
  }

  protected onDismiss(): void {
    this.resetForms();
    this.modalClosed.emit();
  }

  protected formatDate(dateStr: string): string {
    return dayjs(dateStr).format('MMM D, YYYY').toUpperCase();
  }

  protected submitAppointment(): void {
    const userId = this.authService.getActiveUser()?.id;

    if (!userId || this.isSubmitting) return;

    this.isSubmitting = true;

    const editing = this.editingAppointment();
    let payload: Omit<Appointment, 'id'>;

    if (this.appointmentType === 'new') {
      const form = this.newForm.getRawValue();
      payload = {
        category: form.category,
        date: form.date,
        doctor: form.doctor,
        hospital: form.hospital,
        isFollowUp: false,
        location: form.location,
        time: form.time,
        title: form.title,
        userId,
        visited: false,
      };
    } else {
      const form = this.followUpForm.getRawValue();
      const previous = this.appointments().find(
        (a) => a.id === form.previousAppointmentId,
      );

      if (!previous) {
        this.isSubmitting = false;
        return;
      }

      payload = {
        category: previous.category,
        date: form.date || previous.date,
        doctor: form.doctor || previous.doctor,
        followUpInterval: form.interval,
        hospital: form.hospital || previous.hospital,
        isFollowUp: true,
        location: form.location || previous.location,
        previousAppointmentId: previous.id,
        time: form.time || previous.time,
        title: form.title || `${previous.title} Follow-up`,
        userId,
        visited: false,
      };
    }

    const request$ = editing
      ? this.appointmentService.update(editing.id, payload)
      : this.appointmentService.create(payload);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      error: () => {
        this.errorMessage = 'Failed to submit appointment.';
        this.isSubmitting = false;
      },
      next: (result) => {
        this.formSubmitted.emit(result);
        this.isSubmitting = false;
        this.onDismiss();
      },
    });
  }

  private calculateFollowUpDate(): void {
    const form = this.followUpForm.getRawValue();
    const previousId = form.previousAppointmentId;
    const interval = form.interval.trim().toLowerCase();

    if (!previousId || !interval) return;

    const previous = this.appointments().find((a) => a.id === previousId);

    if (!previous) return;

    const match = interval.match(/^(\d+)\s*(day|week|month|year)s?$/);

    if (!match) return;

    const amount = parseInt(match[1]);
    const unit = match[2] as ManipulateType;
    const calculated = dayjs(previous.date)
      .add(amount, unit)
      .format('YYYY-MM-DD');

    if (calculated !== form.date) {
      this.followUpForm.patchValue({ date: calculated }, { emitEvent: false });
    }
  }

  private resetForms(): void {
    this.appointmentType = 'new';
    this.errorMessage = null;
    this.newForm.reset();
    this.followUpForm.reset();
  }
}
