import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Appointment } from '../../../models/med-vault-model';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { AppointmentService } from '../../../services/appointment/appointment-service';
import { DataRefreshService } from '../../../shared/service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-appointments',
  standalone: false,
  templateUrl: './appointments.page.html',
  styleUrls: ['./appointments.page.scss'],
})
export class AppointmentsPage implements OnInit {
  private readonly authService = inject(AuthenticationService);
  private readonly appointmentService = inject(AppointmentService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly appointments = signal<Appointment[]>([]);
  public readonly editingAppointment = signal<Appointment | null>(null);
  public readonly errorMessage = signal<string | null>(null);
  public readonly formOpen = signal<boolean>(false);

  public readonly filteredAppointments = computed((): Appointment[] =>
    this.appointments(),
  );

  ngOnInit(): void {
    const userId = this.authService.getActiveUser()?.id;
    if (!userId) return;

    this.appointmentService
      .getAllByUserId(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to load appointments.'),
        next: (data) => this.appointments.set(data),
      });
  }

  public closeForm(): void {
    this.editingAppointment.set(null);
    this.formOpen.set(false);
  }

  public onAppointmentDeleted(id: string): void {
    this.appointments.update((list) => list.filter((a) => a.id !== id));
    this.dataRefreshService.emitAppointmentsChanged();
  }

  public onAppointmentUpdated(updated: Appointment): void {
    this.appointments.update((list) =>
      list.map((a) => (a.id === updated.id ? updated : a)),
    );
    this.dataRefreshService.emitAppointmentsChanged();
  }

  public onFormSubmitted(result: Appointment): void {
    this.appointments.update((list) => {
      const editing = this.editingAppointment();
      if (editing) {
        return list.map((a) => (a.id === editing.id ? result : a));
      }
      return [...list, result];
    });
    this.dataRefreshService.emitAppointmentsChanged();
  }

  public openEdit(appt: Appointment): void {
    this.editingAppointment.set(appt);
    this.formOpen.set(true);
  }

  public openForm(): void {
    this.editingAppointment.set(null);
    this.formOpen.set(true);
  }
}
