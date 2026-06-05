import {
  Component,
  computed,
  DestroyRef,
  HostBinding,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CalendarDay } from '../../../models/main-layout-model';
import { Appointment, Medicine } from '../../../models/med-vault-model';
import { AppointmentService } from '../../../services/appointment/appointment-service';
import { AuthenticationService } from '../../../services/authentication/authentication-service';
import { MedicineService } from '../../../services/medicine/medicine-service';
import { DataRefreshService } from '../../service/data-refresh/data-refresh-service';

@Component({
  selector: 'app-calendar',
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.scss'],
  standalone: false,
})
export class CalendarComponent implements OnInit {
  public readonly mode = input.required<
    'appointments' | 'dashboard' | 'medicines'
  >();

  private readonly appointmentService = inject(AppointmentService);
  private readonly authService = inject(AuthenticationService);
  private readonly dataRefreshService = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly medicineService = inject(MedicineService);

  protected readonly appointments = signal<Appointment[]>([]);
  protected readonly hoveredDate = signal<CalendarDay | null>(null);
  protected readonly medicines = signal<Medicine[]>([]);
  protected readonly viewDate = signal<Date>(new Date());
  protected readonly weekDays = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

  @HostBinding('class')
  public get modeClass(): string {
    return `mode-${this.mode()}`;
  }

  ngOnInit(): void {
    const userId = this.authService.getActiveUser()?.id;
    if (!userId) return;

    this.loadAppointments(userId);
    this.loadMedicines(userId);

    this.dataRefreshService.appointmentsChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadAppointments(userId));

    this.dataRefreshService.medicinesChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadMedicines(userId));
  }

  protected readonly calendarDays = computed<CalendarDay[]>(() => {
    const date = this.viewDate();
    const year = date.getFullYear();
    const month = date.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const today = new Date();
    const days: CalendarDay[] = [];

    const startPadding = firstDay.getDay();
    for (let i = startPadding - 1; i >= 0; i--) {
      days.push(this.buildDay(new Date(year, month, -i), false, today));
    }

    for (let d = 1; d <= lastDay.getDate(); d++) {
      days.push(this.buildDay(new Date(year, month, d), true, today));
    }

    const remaining = 42 - days.length;
    for (let i = 1; i <= remaining; i++) {
      days.push(this.buildDay(new Date(year, month + 1, i), false, today));
    }

    return days;
  });

  protected readonly monthLabel = computed(() =>
    this.viewDate().toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    }),
  );

  protected formatPopupDate(date: Date): string {
    return date.toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'long',
      weekday: 'long',
    });
  }

  protected nextMonth(): void {
    const d = this.viewDate();
    this.viewDate.set(new Date(d.getFullYear(), d.getMonth() + 1, 1));
  }

  protected onDateLeave(): void {
    this.hoveredDate.set(null);
  }

  protected onDateTap(day: CalendarDay): void {
    if (!day.isCurrentMonth) return;

    const hasEvents = day.appointments.length > 0 || day.medicines.length > 0;
    if (!hasEvents) {
      this.hoveredDate.set(null);
      return;
    }

    this.hoveredDate.set(this.hoveredDate()?.date === day.date ? null : day);
  }

  protected prevMonth(): void {
    const d = this.viewDate();
    this.viewDate.set(new Date(d.getFullYear(), d.getMonth() - 1, 1));
  }

  private buildDay(
    date: Date,
    isCurrentMonth: boolean,
    today: Date,
  ): CalendarDay {
    const dateStr = this.toDateString(date);
    const isToday = this.toDateString(today) === dateStr;

    const appointments = isCurrentMonth
      ? this.appointments().filter((a) => a.date === dateStr)
      : [];

    const medicines = isCurrentMonth
      ? this.medicines().filter(
          (m) => m.startDate <= dateStr && m.endDate >= dateStr,
        )
      : [];

    return { appointments, date, isCurrentMonth, isToday, medicines };
  }

  private loadAppointments(userId: string): void {
    this.appointmentService
      .getAllByUserId(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => this.appointments.set(data));
  }

  private loadMedicines(userId: string): void {
    this.medicineService
      .getAllByUserId(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => this.medicines.set(data));
  }

  private toDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
