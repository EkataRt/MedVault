import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

import {
  Medicine,
  WeeklyConsistency,
} from '../../../../../models/med-vault-model';
import { AppointmentService } from '../../../../../services/appointment/appointment-service';
import { AuthenticationService } from '../../../../../services/authentication/authentication-service';
import { MedicineService } from '../../../../../services/medicine/medicine-service';

Chart.register(...registerables);

@Component({
  selector: 'app-health-analytics',
  templateUrl: './health-analytics.component.html',
  styleUrls: ['./health-analytics.component.scss'],
  standalone: false,
})
export class HealthAnalyticsComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  protected readonly isLoading = signal<boolean>(true);
  protected readonly weeklyData = signal<WeeklyConsistency[]>([]);

  private chart: Chart | null = null;
  private userId: string = '';

  constructor(
    private appointmentService: AppointmentService,
    private auth: AuthenticationService,
    private medicineService: MedicineService,
  ) {}

  ngOnInit(): void {
    this.userId = this.auth.getActiveUser()?.id ?? '';

    if (this.userId) {
      this.loadData();
    } else {
      this.isLoading.set(false);
    }
  }

  ngAfterViewInit(): void {
    if (this.weeklyData().length > 0) {
      this.renderChart();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private buildWeeks(): { label: string; start: Date; end: Date }[] {
    const now = new Date();
    const year = now.getFullYear();
    const month = now.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const weeks: { label: string; start: Date; end: Date }[] = [];
    let weekStart = new Date(firstDay);
    let weekNum = 1;

    while (weekStart <= lastDay) {
      const weekEnd = new Date(weekStart);

      weekEnd.setDate(weekStart.getDate() + 6);

      if (weekEnd > lastDay) {
        weekEnd.setTime(lastDay.getTime());
      }

      weeks.push({
        end: weekEnd,
        label: `Week ${weekNum}`,
        start: new Date(weekStart),
      });

      weekStart.setDate(weekStart.getDate() + 7);
      weekNum++;
    }

    return weeks;
  }

  private computeAppointmentConsistency(
    appointments: { date: string; visited: boolean; visitedDate?: string }[],
    start: Date,
    end: Date,
  ): number {
    const startStr = start.toISOString().split('T')[0];
    const endStr = end.toISOString().split('T')[0];

    const inWindow = appointments.filter((a) => {
      const ref = a.visitedDate ?? a.date;

      return ref >= startStr && ref <= endStr;
    });

    if (inWindow.length === 0) return 0;

    const visited = inWindow.filter((a) => a.visited).length;

    return Math.round((visited / inWindow.length) * 100);
  }

  private computeMedicineConsistency(
    medicines: Medicine[],
    start: Date,
    end: Date,
  ): number {
    const startStr = start.toISOString().split('T')[0];
    const endStr = end.toISOString().split('T')[0];
    const today = new Date().toISOString().split('T')[0];

    const activeMedicines = medicines.filter(
      (m) => m.startDate <= endStr && m.endDate >= startStr,
    );

    if (activeMedicines.length === 0) return 0;

    let totalDoses = 0;
    let takenDoses = 0;

    for (const medicine of activeMedicines) {
      const daysInWindow = this.countDaysInWindow(
        medicine.startDate,
        medicine.endDate,
        startStr,
        endStr,
      );

      totalDoses += daysInWindow * medicine.times.length;

      const windowContainsToday = startStr <= today && today <= endStr;

      if (windowContainsToday) {
        const takenToday = (medicine.lastTakenDates ?? []).filter(
          (d) => d === today,
        ).length;

        takenDoses += takenToday;
      }
    }

    if (totalDoses === 0) return 0;

    return Math.round((takenDoses / totalDoses) * 100);
  }

  private countDaysInWindow(
    medicineStart: string,
    medicineEnd: string,
    windowStart: string,
    windowEnd: string,
  ): number {
    const start = new Date(
      Math.max(
        new Date(medicineStart).getTime(),
        new Date(windowStart).getTime(),
      ),
    );
    const end = new Date(
      Math.min(new Date(medicineEnd).getTime(), new Date(windowEnd).getTime()),
    );

    if (start > end) return 0;

    return (
      Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1
    );
  }

  private loadData(): void {
    let appointmentsLoaded = false;
    let medicinesLoaded = false;

    const appointments: {
      date: string;
      visited: boolean;
      visitedDate?: string;
    }[] = [];

    const medicines: Medicine[] = [];

    const tryBuild = (): void => {
      if (!appointmentsLoaded || !medicinesLoaded) return;

      const weeks = this.buildWeeks();
      const data: WeeklyConsistency[] = weeks.map((week) => ({
        appointmentConsistency: this.computeAppointmentConsistency(
          appointments,
          week.start,
          week.end,
        ),
        medicineConsistency: this.computeMedicineConsistency(
          medicines,
          week.start,
          week.end,
        ),
        week: week.label,
      }));

      this.weeklyData.set(data);
      this.isLoading.set(false);
      setTimeout(() => this.renderChart(), 0);
    };

    this.appointmentService.getAllByUserId(this.userId).subscribe({
      error: () => {
        appointmentsLoaded = true;
        tryBuild();
      },
      next: (data) => {
        appointments.push(...data);
        appointmentsLoaded = true;
        tryBuild();
      },
    });

    this.medicineService.getAllByUserId(this.userId).subscribe({
      error: () => {
        medicinesLoaded = true;
        tryBuild();
      },
      next: (data) => {
        medicines.push(...data);
        medicinesLoaded = true;
        tryBuild();
      },
    });
  }

  private renderChart(): void {
    if (!this.chartCanvas) return;

    this.chart?.destroy();

    const data = this.weeklyData();
    const labels = data.map((d) => d.week);
    const appointmentData = data.map((d) => d.appointmentConsistency);
    const medicineData = data.map((d) => d.medicineConsistency);

    const isDark = document.body.classList.contains('dark');

    // Read computed styles directly from the canvas wrapper to ensure inherited CSS variables are resolved correctly
    const canvasEl = this.chartCanvas.nativeElement;
    const computedStyles = getComputedStyle(
      canvasEl.parentElement || document.body,
    );

    const warningColor =
      computedStyles.getPropertyValue('--ion-color-warning').trim() ||
      '#b45309';
    const successColor =
      computedStyles.getPropertyValue('--ion-color-success').trim() ||
      '#2d7a5d';

    // Solid high-contrast text and axis colors (NO faint alpha blending)
    const textColor = isDark ? '#f5f5f5' : '#261638';
    const tickColor = isDark ? '#a0a0a0' : '#524366'; // Dark purple-grey in light mode for crisp readability
    const gridColor = isDark
      ? 'rgba(255, 255, 255, 0.1)'
      : 'rgba(38, 22, 56, 0.08)';

    const config: ChartConfiguration<'bar'> = {
      data: {
        datasets: [
          {
            backgroundColor: warningColor,
            borderRadius: 6,
            borderSkipped: false,
            data: appointmentData,
            label: 'Appointment Consistency',
          },
          {
            backgroundColor: successColor,
            borderRadius: 6,
            borderSkipped: false,
            data: medicineData,
            label: 'Medicine Consistency',
          },
        ],
        labels,
      },
      options: {
        animation: { duration: 600, easing: 'easeInOutQuart' },
        interaction: { intersect: false, mode: 'index' },
        maintainAspectRatio: false,
        plugins: {
          legend: {
            align: 'center',
            labels: {
              boxHeight: 10,
              boxWidth: 10,
              color: textColor,
              font: { family: 'DM Sans', size: 12 },
              padding: 16,
              usePointStyle: true,
            },
            position: 'top',
          },
          tooltip: {
            backgroundColor: isDark ? '#1e1e1e' : '#ffffff',
            bodyColor: textColor,
            bodyFont: { family: 'DM Sans', size: 12 },
            borderColor: isDark
              ? 'rgba(255,255,255,0.12)'
              : 'rgba(66, 42, 92, 0.15)',
            borderWidth: 1,
            callbacks: {
              label: (ctx) => ` ${ctx.dataset.label}: ${ctx.parsed.y}%`,
            },
            padding: 10,
            titleColor: textColor,
            titleFont: { family: 'DM Sans', size: 12, weight: 'bold' },
          },
        },
        responsive: true,
        scales: {
          x: {
            border: { display: false },
            grid: { display: false },
            ticks: {
              color: tickColor,
              font: { family: 'DM Sans', size: 12 },
              maxRotation: 0,
            },
          },
          y: {
            border: { display: false },
            grid: { color: gridColor },
            max: 100,
            min: 0,
            ticks: {
              callback: (value) => `${value}%`,
              color: tickColor,
              font: { family: 'DM Sans', size: 11 },
              maxTicksLimit: 6,
              stepSize: 20,
            },
            title: {
              color: tickColor,
              display: true,
              font: { family: 'DM Sans', size: 11 },
              text: 'Consistency (%)',
            },
          },
        },
      },
      type: 'bar',
    };

    this.chart = new Chart(canvasEl, config);
  }
}
