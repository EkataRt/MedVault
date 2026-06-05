import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Appointment } from '../../../../../models/med-vault-model';
import { AppointmentService } from '../../../../../services/appointment/appointment-service';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { DataRefreshService } from '../../../../../shared/service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-appointment-list',
  templateUrl: './appointment-list.component.html',
  styleUrls: ['./appointment-list.component.scss'],
  standalone: false,
})
export class AppointmentListComponent implements OnInit {
  private readonly appointmentService = inject(AppointmentService);
  private readonly auth = inject(AuthenticationService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  protected readonly appointments = signal<Appointment[]>([]);
  protected readonly isLoading = signal<boolean>(true);

  ngOnInit(): void {
    const userId = this.auth.getActiveUser()?.id;
    if (!userId) {
      this.isLoading.set(false);
      return;
    }

    this.load(userId);

    this.dataRefreshService.appointmentsChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load(userId));
  }

  protected formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);

    if (date.toDateString() === today.toDateString()) return 'Today';
    if (date.toDateString() === tomorrow.toDateString()) return 'Tomorrow';

    return date.toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
    });
  }

  protected navigateToAppointments(): void {
    this.router.navigate(['/main/appointments']);
  }

  private load(userId: string): void {
    this.isLoading.set(true);
    this.appointmentService
      .getUpcomingByUserId(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.isLoading.set(false),
        next: (data) => {
          this.appointments.set(data);
          this.isLoading.set(false);
        },
      });
  }
}
