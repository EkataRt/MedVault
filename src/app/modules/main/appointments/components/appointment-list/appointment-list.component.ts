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

import { Appointment } from '../../../../../models/med-vault-model';
import { AppointmentService } from '../../../../../services/appointment/appointment-service';

@Component({
  selector: 'app-appointment-list',
  standalone: false,
  templateUrl: './appointment-list.component.html',
  styleUrl: './appointment-list.component.scss',
})
export class AppointmentListComponent {
  private readonly appointmentService = inject(AppointmentService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly appointments = input.required<Appointment[]>();

  public readonly appointmentDeleted = output<string>();
  public readonly appointmentUpdated = output<Appointment>();
  public readonly editRequested = output<Appointment>();

  public readonly deleteTargetId = signal<string | null>(null);
  public readonly errorMessage = signal<string | null>(null);

  public formatDate(dateStr: string): string {
    return dayjs(dateStr).format('MMM D, YYYY').toUpperCase();
  }

  public openDeleteConfirm(id: string): void {
    this.deleteTargetId.set(id);
  }

  public onDeleteConfirmed(): void {
    const id = this.deleteTargetId();

    if (!id) return;

    this.appointmentService
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to delete appointment.'),
        next: () => {
          this.appointmentDeleted.emit(id);
          this.deleteTargetId.set(null);
        },
      });
  }

  public onDeleteCancelled(): void {
    this.deleteTargetId.set(null);
  }

  public onEditRequested(appt: Appointment): void {
    this.editRequested.emit(appt);
  }

  public toggleVisited(appt: Appointment): void {
    const visited = !appt.visited;
    const patch = visited
      ? { visited, visitedDate: dayjs().format('YYYY-MM-DD') }
      : { visited, visitedDate: undefined };

    this.appointmentService
      .update(appt.id, patch)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.errorMessage.set('Failed to update appointment.'),
        next: (result) => this.appointmentUpdated.emit(result),
      });
  }

  public deleteTarget(): Appointment | null {
    const id = this.deleteTargetId();

    if (!id) return null;

    return this.appointments().find((a) => a.id === id) ?? null;
  }
}
